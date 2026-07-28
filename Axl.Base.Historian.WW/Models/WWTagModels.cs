using System;
using System.Collections.Generic;

namespace Axl.Base.Historian.WW.Models
{
    public class AreTag
    {
        public string Area { get; set; }
        public AreTag()
        {
            Area = string.Empty;
        }
    }

    public class SupTag
    {
        public int Id { get; set; }
        public string Tag { get; set; }
        public string TagName { get; set; }
        public string Area { get; set; }
        public int StationId { get; set; }
        public string StationName { get; set; }
        public int Inhibido { get; set; }
        public DateTime FechaUpdate { get; set; }
        public DateTime FechaInhibit { get; set; }
        public ValTag Resultado { get; set; }
        
        public SupTag() 
        { 
            Id = 0;
            Tag = string.Empty;
            TagName = string.Empty;
            StationId = 0;
            StationName = string.Empty;
            Area = string.Empty;
            Resultado = null;
        }
    }

    public class ValTag
    {
        public string Tag { get; set; }
        public DateTime Actualizacion { get; set; }
        public double? Valor { get; set; }
        public string Unit { get; set; }
        public int Quality { get; set; }
        public int QualityDetail { get; set; }
        
        public ValTag()
        {
            Tag = string.Empty;
            Actualizacion = DateTime.MinValue;
            Valor = null;
            Unit = string.Empty;
            QualityDetail = 0;
            Quality = 0;
        }
    }

    public class WWResponse
    {
        public string Tag { get; set; }
        public string TagKey { get; set; }
        public string Mode { get; set; }
        public string TimeDB { get; set; }
        public string ValueDB { get; set; }
        public string TimeZone { get; set; }
        public string Parameter { get; set; }
        public string Selector { get; set; }
    }
}
