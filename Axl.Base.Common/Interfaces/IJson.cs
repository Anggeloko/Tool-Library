﻿namespace Axl.Base.Interfaces
{
    public interface IJson
    {
        string Serialize(object obj);
        T Deserialize<T>(string json);
    }
}

