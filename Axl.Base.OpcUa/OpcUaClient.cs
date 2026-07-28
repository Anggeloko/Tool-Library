﻿using Opc.Ua.Client;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Axl.Base.Models;
using Axl.Base.Statics;
using Axl.Base.Interfaces;
using System.Threading;

namespace Axl.Base.OpcUa
{
    public class OpcUaClient : IOpc
    {
        private readonly ApplicationConfiguration _config;
        private Session _session;
        private readonly string _endpointUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly int _timeout;
        private bool _disposed;

        public OpcUaClient(string endpointUrl, int timeout = 5000, string username = null, string password = null)
        {
            _endpointUrl = endpointUrl ?? throw new ArgumentNullException(nameof(endpointUrl));
            _timeout = timeout;
            _username = username;
            _password = password;

            _config = new ApplicationConfiguration
            {
                ApplicationName = "OpcUaClient",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier(),
                    TrustedPeerCertificates = new CertificateTrustList(),
                    TrustedIssuerCertificates = new CertificateTrustList(),
                    RejectedCertificateStore = new CertificateTrustList(),
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = timeout },
                ClientConfiguration = new ClientConfiguration()
            };
        }

        public async Task ConnectAsync()
        {
            if (_session != null && _session.Connected)
                return;

            try
            {
                EndpointDescription endpointDescription = await DiscoverEndpointAsync();
                EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(_config);
                ConfiguredEndpoint endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

                UserIdentity userIdentity;
                if (string.IsNullOrEmpty(_username))
                {
                    userIdentity = new UserIdentity();
                }
                else
                {
                    userIdentity = new UserIdentity(_username, Encoding.UTF8.GetBytes(_password ?? string.Empty));
                }

                // Final Solution: Use DefaultSessionFactory with named parameter to avoid obsolete warnings
                var sessionFactory = new DefaultSessionFactory(telemetry: null);
                _session = (Session)await sessionFactory.CreateAsync(
                    _config,
                    (ITransportWaitingConnection)null,
                    endpoint,
                    true,
                    false,
                    "OpcUaClientSession",
                    (uint)_timeout,
                    userIdentity,
                    null,
                    CancellationToken.None);

                if (!_session.Connected)
                    throw new Exception("Failed to connect to OPC UA server.");
            }
            catch (Exception ex)
            {
                throw new Exception($"[ERROR(OPC UA)] Connection failed: {ex.Message} [Endpoint: {_endpointUrl}]");
            }
        }

        private async Task<EndpointDescription> DiscoverEndpointAsync()
        {
            try
            {
                // Final Solution: Use the modern SelectEndpointAsync
                var selectedEndpoint = await CoreClientUtils.SelectEndpointAsync(
                    telemetry: null,
                    application: _config,
                    discoveryUrl: _endpointUrl,
                    useSecurity: false,
                    discoverTimeout: _timeout,
                    ct: CancellationToken.None);
                return selectedEndpoint ?? throw new Exception($"Could not select endpoint for {_endpointUrl}");
            }
            catch (Exception ex)
            {
                throw new Exception($"[ERROR(OPC UA)] Discovery failed: {ex.Message} [Endpoint: {_endpointUrl}]");
            }
        }

        public async Task<Result<Dictionary<string, string>>> ReadNodesAsync(List<string> nodeIds)
        {
            var result = new Dictionary<string, string>();
            var errors = new List<string>();

            if (_session == null || !_session.Connected)
            {
                errors.Add("[ERROR(OPC UA)] Not connected to the server.");
                return Result<Dictionary<string, string>>.Failure(errors);
            }

            try
            {
                var readValueIds = new ReadValueIdCollection();
                foreach (var nodeId in nodeIds)
                {
                    readValueIds.Add(new ReadValueId
                    {
                        NodeId = new NodeId(nodeId),
                        AttributeId = Attributes.Value
                    });
                }

                var response = await _session.ReadAsync(
                    null,
                    0,
                    TimestampsToReturn.Both,
                    readValueIds,
                    CancellationToken.None);

                var values = response.Results;

                for (int i = 0; i < values.Count; i++)
                {
                    var value = values[i];
                    if (StatusCode.IsGood(value.StatusCode))
                    {
                        result[nodeIds[i]] = value.Value?.ToString() ?? "null";
                    }
                    else
                    {
                        errors.Add($"[ERROR(OPC UA)] Failed to read node {nodeIds[i]}: {value.StatusCode}");
                    }
                }

                return errors.Count > 0
                    ? Result<Dictionary<string, string>>.Failure(errors)
                    : Result<Dictionary<string, string>>.Success(result);
            }
            catch (Exception ex)
            {
                errors.Add($"[ERROR(OPC UA)] Exception during read: {ex.Message}");
                return Result<Dictionary<string, string>>.Failure(errors);
            }
        }

        public async Task<Result<Dictionary<string, string>>> BrowseNodesAsync(string rootNodeId)
        {
            var result = new Dictionary<string, string>();
            var errors = new List<string>();

            if (_session == null || !_session.Connected)
            {
                errors.Add("[ERROR(OPC UA)] Not connected to the server.");
                return Result<Dictionary<string, string>>.Failure(errors);
            }

            try
            {
                var browser = new Browser(_session)
                {
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = (int)(NodeClass.Object | NodeClass.Variable),
                    ContinueUntilDone = true
                };

                var rootNode = new NodeId(rootNodeId);
                ReferenceDescriptionCollection references = await browser.BrowseAsync(rootNode);

                foreach (var reference in references)
                {
                    var nodeId = ExpandedNodeId.ToNodeId(reference.NodeId, _session.NamespaceUris);
                    var nodeValue = await _session.ReadValueAsync(nodeId);
                    
                    if (StatusCode.IsGood(nodeValue.StatusCode))
                    {
                        result[nodeId.ToString()] = nodeValue.Value?.ToString() ?? "null";
                    }
                    else
                    {
                        errors.Add($"[ERROR(OPC UA)] Failed to read node {nodeId}: {nodeValue.StatusCode}");
                    }
                }

                return errors.Count > 0
                    ? Result<Dictionary<string, string>>.Failure(errors)
                    : Result<Dictionary<string, string>>.Success(result);
            }
            catch (Exception ex)
            {
                errors.Add($"[ERROR(OPC UA)] Exception during browse: {ex.Message}");
                return Result<Dictionary<string, string>>.Failure(errors);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _session?.CloseAsync().Wait(2000);
            }
            catch { /* Ignore closure errors on disposal */ }
            
            _session?.Dispose();
            _disposed = true;
        }
    }
}

