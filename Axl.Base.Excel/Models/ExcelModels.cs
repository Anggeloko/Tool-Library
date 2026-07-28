﻿using System;
using System.Collections.Generic;
using System.Linq;
using Axl.Base.Statics;

namespace Axl.Base.Models
{
    public class ExcSheet 
    { 
        public string SharepointPath { get; set; }
        public string LocalPath { get; set; }
        public List<ElShe> Sheets { get; set; }
    }

    public class ElShe 
    {
        public string Sheet { get; set; }
        public int Model { get; set; }
    }

    public class BaS
    {
        public string Table { get; set; }
        public int Sheet { get; set; }
        public List<BaC> Columns { get; set; }
        public List<List<string>> Values { get; set; }

        public BaS()
        {
            Table = string.Empty;
            Sheet = -1;
            Columns = new List<BaC>();
            Values = new List<List<string>>();
        }
    }

    public class BaC
    {
        public string Column { get; set; }
        public int Index { get; set; }
        public string DType => Voting.Where(kv => kv.Key != "dbnull" && kv.Key != "object")
                                     .OrderByDescending(kv => kv.Value)
                                     .Select(kv => kv.Key)
                                     .FirstOrDefault();
        public Dictionary<string, int> Voting { get; set; }

        public BaC()
        {
            Column = string.Empty;
            Index = 0;
            Voting = new Dictionary<string, int>();
        }

        public string DataValue(object v)
        {
            if (v == null || v == DBNull.Value) return "[null]";
            
            string dtype = DType?.ToLower() ?? "string";
            string currentType = v.GetType().Name.ToLower();

            // Si el tipo detectado coincide con el valor actual, o si es la primera vez (DType null)
            switch (dtype)
            {
                case "string":
                    return v.ToString();
                case "datetime":
                    return v is DateTime dt ? dt.ToLongDateString() : v.ToString();
                case "int":
                case "int16":
                case "int32":
                case "int64":
                    return v.ToString();
                case "single":
                case "double":
                case "decimal":
                    return v is double d ? d.Str() : v.ToString();
                default:
                    return v.ToString();
            }
        }

        public string DBDataType()
        {
            if (DType == null) return "varchar(MAX)";
            switch (DType.ToLower())
            {
                case "datetime":
                case "single":
                case "double":
                case "decimal":
                case "string":
                    return "varchar(MAX)";
                case "int":
                case "int16":
                case "int32":
                case "int64":
                    return "int";
                default:
                    return "varchar(MAX)";
            }
        }
    }
}

