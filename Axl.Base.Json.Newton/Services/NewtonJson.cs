﻿using Newtonsoft.Json;
using Axl.Base.Interfaces;

namespace Axl.Base.Json.Newton.Services
{
    public class NewtonJson : IJson
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonJson()
        {
            _settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                // Replicamos el comportamiento de JavaScriptSerializer para compatibilidad legacy
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                // Newtonsoft no tiene un MaxJsonLength artificial como JavaScriptSerializer,
                // pero aumentamos el MaxDepth por si hay estructuras muy anidadas.
                MaxDepth = 1024 
            };
        }

        public string Serialize(object obj)
        {
            if (obj == null) return null;
            return JsonConvert.SerializeObject(obj, _settings);
        }

        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default(T);
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
    }
}


