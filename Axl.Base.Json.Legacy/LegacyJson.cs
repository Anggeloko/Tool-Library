﻿using System;
using System.Web.Script.Serialization;
using Axl.Base.Interfaces;

namespace Axl.Base.Json.Legacy
{
    public class LegacyJson : IJson
    {
        private readonly JavaScriptSerializer _serializer;

        public LegacyJson()
        {
            // Replicamos la configuración que tenías en tu Singleton
            _serializer = new JavaScriptSerializer()
            {
                MaxJsonLength = Int32.MaxValue
            };
        }
        public string Serialize(object obj) =>  _serializer.Serialize(obj);
        public T Deserialize<T>(string json) => _serializer.Deserialize<T>(json);
    }
}

