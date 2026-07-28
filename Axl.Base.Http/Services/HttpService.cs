﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Axl.Base.Http.Interfaces;
using Axl.Base.Http.Models;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Http.Services
{
    /// <summary>
    /// High-performance HTTP client service with automatic authentication and generic CRUD operations.
    /// Supports .NET Framework 4.5.2+ and connection pooling.
    /// </summary>
    public class HttpService : BaseHttpClient, IHttpService, ICheckable
    {
        private readonly HttpConfig _config;
        private readonly ILog _log;
        private string _token = string.Empty;
        private DateTime _tokenExpiration = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpService"/> class.
        /// </summary>
        /// <param name="config">Configuration settings for the service.</param>
        /// <param name="log">Optional logger implementation.</param>
        public HttpService(HttpConfig config, ILog log = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _log = log;
            EnsureDeveloperMode(_config.IsDeveloperMode);
        }

        /// <summary>
        /// Performs a lightweight connectivity check against the BaseUrl.
        /// </summary>
        /// <returns>A result indicating if the server is reachable.</returns>
        public async Task<Result<bool>> CheckAsync()
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds)))
                {
                    var response = await Client.GetAsync(_config.BaseUrl, cts.Token);
                    return Result<bool>.Success(response.IsSuccessStatusCode);
                }
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ExceptionUtils.Format(ex, "HttpCheck"));
            }
        }

        #region --- Token Management ---

        /// <summary>
        /// Manually retrieves or refreshes the authentication token using dynamic field mapping.
        /// </summary>
        /// <returns>A result containing the Bearer token.</returns>
        public async Task<Result<string>> GetTokenAsync()
        {
            if (!string.IsNullOrEmpty(_token) && DateTime.Now < _tokenExpiration)
            {
                return Result<string>.Success(_token);
            }

            try
            {
                var authUrl = CombineUrl(_config.BaseUrl, _config.AuthEndpoint);
                
                // Build dynamic payload for auth request
                var payload = new Dictionary<string, string>
                {
                    { _config.UserField, _config.Username },
                    { _config.PasswordField, _config.Password }
                };
                
                var response = await SendBaseAsync<Dictionary<string, object>>(HttpMethod.Post, authUrl, payload, useAuth: false);
                
                if (!response.IsSuccess)
                    return Result<string>.Failure(response.Errors);

                var data = response.Value;
                
                // Use dynamic mapping for auth response
                if (data.TryGetValue(_config.TokenKey, out var tokenObj) && data.TryGetValue(_config.ExpirationKey, out var expiresObj))
                {
                    _token = tokenObj.ToString();
                    
                    // Manejar expiración: Segundos relativos, Unix Timestamp o ISO Date
                    string rawExpires = expiresObj.ToString();
                    if (long.TryParse(rawExpires, out long unixValue))
                    {
                        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        if (unixValue > 2000000000000) // Milisegundos Unix
                            _tokenExpiration = epoch.AddMilliseconds(unixValue).ToLocalTime().AddMinutes(-1);
                        else if (unixValue > 1000000000) // Segundos Unix
                            _tokenExpiration = epoch.AddSeconds(unixValue).ToLocalTime().AddMinutes(-1);
                        else // Segundos relativos
                            _tokenExpiration = DateTime.Now.AddSeconds(unixValue - 60);
                    }
                    else if (DateTime.TryParse(rawExpires, out DateTime expiresDt))
                    {
                        _tokenExpiration = expiresDt.AddMinutes(-1);
                    }
                    else
                    {
                        _tokenExpiration = DateTime.Now.AddHours(1); // Fallback
                    }

                    return Result<string>.Success(_token);
                }

                _log?.Error($"Invalid token response format. Expected keys: {_config.TokenKey}, {_config.ExpirationKey}. Found: {string.Join(", ", data.Keys)}");
                return Result<string>.Failure("Invalid token response format.");
            }
            catch (Exception ex)
            {
                _log?.Error($"Token retrieval failed: {ex.Message}");
                return Result<string>.Failure(ExceptionUtils.Format(ex, "GetToken"));
            }
        }

        #endregion

        #region --- Generic CRUD Methods ---

        public Task<Result<TResponse>> GetAsync<TResponse>(string endpoint) => 
            SendAsync<object, TResponse>(HttpMethod.Get, endpoint, null);

        public Task<Result<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest body) => 
            SendAsync<TRequest, TResponse>(HttpMethod.Post, endpoint, body);

        public Task<Result<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest body) => 
            SendAsync<TRequest, TResponse>(HttpMethod.Put, endpoint, body);

        public Task<Result<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest body) => 
            SendAsync<TRequest, TResponse>(new HttpMethod("PATCH"), endpoint, body);

        public async Task<Result<bool>> DeleteAsync(string endpoint)
        {
            var response = await SendAsync<object, object>(HttpMethod.Delete, endpoint, null);
            return Result<bool>.Success(response.IsSuccess);
        }

        #endregion

        #region --- File Upload Methods ---

        public async Task<Result<bool>> UploadFileAsync(string filePath, string endpoint)
        {
            try
            {
                string token = null;
                if (_config.UseAuth)
                {
                    var tokenResult = await GetTokenAsync();
                    if (!tokenResult.IsSuccess) return Result<bool>.Failure(tokenResult.Errors);
                    token = tokenResult.Value;
                }

                if (!File.Exists(filePath)) return Result<bool>.Failure($"File not found: {filePath}");

                var uploadUrl = CombineUrl(_config.BaseUrl, endpoint);

                using (var content = new MultipartFormDataContent())
                {
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var fileContent = new StreamContent(fileStream);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        content.Add(fileContent, "file", Path.GetFileName(filePath));

                        using (var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl))
                        {
                            if (!string.IsNullOrEmpty(_config.UserAgent))
                                request.Headers.UserAgent.ParseAdd(_config.UserAgent);

                            request.Content = content;
                            if (_config.UseAuth && !string.IsNullOrEmpty(token))
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds)))
                            {
                                var response = await Client.SendAsync(request, cts.Token);
                                if (response.IsSuccessStatusCode)
                                {
                                    _log?.Info($"Successfully uploaded {filePath} to {uploadUrl}");
                                    return Result<bool>.Success(true);
                                }

                                return Result<bool>.Failure($"Upload failed with status: {response.StatusCode}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"File upload failed: {ex.Message}");
                return Result<bool>.Failure(ExceptionUtils.Format(ex, "UploadFile"));
            }
        }

        #endregion

        #region --- Core Send Methods ---

        private async Task<Result<TResponse>> SendAsync<TRequest, TResponse>(HttpMethod method, string endpoint, TRequest body)
        {
            string token = null;
            if (_config.UseAuth)
            {
                var tokenResult = await GetTokenAsync();
                if (!tokenResult.IsSuccess) return Result<TResponse>.Failure(tokenResult.Errors);
                token = tokenResult.Value;
            }

            var url = CombineUrl(_config.BaseUrl, endpoint);
            return await SendBaseAsync<TResponse>(method, url, body, useAuth: _config.UseAuth, token: token);
        }

        private async Task<Result<TResponse>> SendBaseAsync<TResponse>(HttpMethod method, string url, object body, bool useAuth = false, string token = null)
        {
            try
            {
                using (var request = new HttpRequestMessage(method, url))
                {
                    if (!string.IsNullOrEmpty(_config.UserAgent))
                        request.Headers.UserAgent.ParseAdd(_config.UserAgent);

                    if (useAuth && !string.IsNullOrEmpty(token))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    if (body != null)
                    {
                        var json = JsonConvert.SerializeObject(body);
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    }

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds)))
                    {
                        var response = await GetClient().SendAsync(request, cts.Token);
                        var content = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            if (typeof(TResponse) == typeof(object))
                                return Result<TResponse>.Success(default);

                            var data = JsonConvert.DeserializeObject<TResponse>(content);
                            return Result<TResponse>.Success(data);
                        }

                        _log?.Error($"HTTP {method} {url} failed: {response.StatusCode} - {content}");
                        return Result<TResponse>.Failure($"HTTP Error {response.StatusCode}: {content}");
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Failure(ExceptionUtils.Format(ex, $"Http{method}"));
            }
        }

        private string CombineUrl(string basePart, string endPart)
        {
            if (string.IsNullOrEmpty(basePart)) return endPart;
            if (string.IsNullOrEmpty(endPart)) return basePart;
            return $"{basePart.TrimEnd('/')}/{endPart.TrimStart('/')}";
        }

        #endregion
    }
}

