﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TitaniumAS.Opc.Client.Common;
using TitaniumAS.Opc.Client.Da;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.OpcDa.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using TitaniumAS.Opc.Client.Da; // Aseg�rate de tener esta referencia

    public class OpcDaService : IOpcDaService
    {
        private readonly int _delayms = 500;
        public OpcDaService(int delay = 500) 
        { 
            _delayms = delay;
        }
        public OPCRes Read(string deviceId, string svrName, string ip, Dictionary<string, string> parameters, out string error, int retry = 1)
        {
            OPCRes result = new OPCRes();
            error = string.Empty;

            // Validaci�n inicial
            if (parameters == null || parameters.Count == 0)
            {
                error = "[OPC] No se proporcionaron par�metros de lectura.";
                return result;
            }

            // 1. BLOQUEO EXCLUSIVO (Protecci�n del recurso por DeviceId)
            if (!TrafficControl.StartExclusiveHeavy(deviceId))
            {
                error = $"[OPC] {deviceId}: El equipo est� ocupado (Timeout de bloqueo).";
                return result;
            }

            try
            {
                Uri url = UrlBuilder.Build(svrName, ip);
                using (OpcDaServer opcsvr = new OpcDaServer(url))
                {
                    opcsvr.Connect();
                    OpcDaGroup group = opcsvr.AddGroup("DynamicMonitoringGroup");
                    group.IsActive = true;

                    // 2. DEFINICI�N DE ITEms
                    var definitions = parameters.Select(p => new OpcDaItemDefinition
                    {
                        ItemId = p.Value,
                        IsActive = true
                    }).ToList();

                    // 3. CORRECCI�N ERROR CS0029: AddItems devuelve OpcDaItemResult[]
                    var addResults = group.AddItems(definitions.ToArray());

                    // SOLO agregamos a pendientes los que el servidor confirm� como v�lidos (Ruta correcta)
                    List<OpcDaItem> pendingItems = addResults
                        .Where(r => r.Error.Succeeded) // <--- Filtro cr�tico
                        .Select(r => r.Item)
                        .ToList();

                    // Opcional: Si quieres saber cu�les fallaron por ruta err�nea
                    var failedTags = addResults.Where(r => !r.Error.Succeeded).ToList();
                    if (failedTags.Count > 0)
                    {
                        error = $"[ERROR ROUTE]: {failedTags.Count} tags no existen en el servidor. ";
                    }

                    // Extraemos los objetos OpcDaItem v�lidos para la lectura
                    //List<OpcDaItem> pendingItems = addResults.Select(r => r.Item).ToList();

                    int currentTry = 0;

                    // 4. BUCLE DE LECTURA INCREMENTAL
                    // Solo reintentamos los elementos que no tengan calidad "Good"
                    while (pendingItems.Count > 0 && currentTry <= retry)
                    {
                        // Leemos solo los que a�n est�n en la lista de pendientes
                        OpcDaItemValue[] values = group.Read(pendingItems.ToArray(), OpcDaDataSource.Device);

                        List<OpcDaItem> solvedThisTurn = new List<OpcDaItem>();

                        foreach (var val in values)
                        {
                            // Si la calidad es aceptable (Good)
                            if (val.Quality.ToString().ToLower().Contains("good"))
                            {
                                // Buscamos el nombre amigable (Key) usando el ItemId (Value)
                                string friendlyName = parameters.FirstOrDefault(x => x.Value == val.Item.ItemId).Key;

                                result.Resultado.Add(new OPCObj
                                {
                                    Nombre = friendlyName,
                                    ValObj = val.Value,
                                    Fecha = val.Timestamp.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                    Calidad = val.Quality.ToString()
                                });

                                // Marcamos como solucionado para no volverlo a pedir
                                solvedThisTurn.Add(val.Item);
                            }
                        }

                        // Limpiamos la lista de pendientes eliminando los que ya le�mos bien
                        foreach (var item in solvedThisTurn)
                        {
                            pendingItems.Remove(item);
                        }

                        // Si a�n faltan items, esperamos un momento antes del reintento
                        if (pendingItems.Count > 0)
                        {
                            currentTry++;
                            if (currentTry <= retry) Thread.Sleep(150);
                        }
                    }

                    // Si al final del bucle quedan pendientes, informamos en el error
                    if (pendingItems.Count > 0)
                    {
                        error = $"[OPC] {deviceId}: {pendingItems.Count} tags no respondieron con calidad Good tras {retry} reintentos.";
                    }

                    opcsvr.Disconnect();
                }
            }
            catch (Exception ex)
            {
                error = $"[ERROR CR�TICO OPC] {deviceId}: {ex.Message}";
            }
            finally
            {
                // 5. LIMPIEZA Y LIBERACI�N DEL PORTERO
                Thread.Sleep(_delayms);
                TrafficControl.StopExclusiveHeavy(deviceId);
            }

            return result;
        }
    }
}


