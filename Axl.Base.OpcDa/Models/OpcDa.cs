﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axl.Base.Models
{
    public class OPCRes
    {
        public List<OPCObj> Resultado { get; set; }
        public List<string> Falla { get; set; }
        public List<string> Alarma { get; set; }
        public List<string> Evento { get; set; }
        public List<string> Info { get; set; }
        public OPCRes()
        {
            Resultado = new List<OPCObj>();
            Falla = new List<string>(); Alarma = new List<string>(); Evento = new List<string>(); Info = new List<string>();
        }
    }
    public class OPCObj
    {
        public string Nombre { get; set; }
        public object ValObj { get; set; }
        public string Fecha { get; set; }
        public string Calidad { get; set; }
        public OPCObj()
        {
            Calidad = "BAD";
        }
        public string Valor
        {
            get { return ValObj?.ToString() ?? "NULL"; }
        }
    }
    public class OPCVariable
    {
        public string Variable { get; set; }
        public int Tipo { get; set; }
        public List<OPCElement> Dict { get; set; }
        public OPCVariable()
        {
            Dict = new List<OPCElement>();
            Variable = ""; Tipo = -1;
        }
    }
    public class OPCElement
    {
        public int Numero { get; set; }
        public int Tipo { get; set; }
        public string Texto { get; set; }
        public OPCElement()
        {
            Numero = -1; Tipo = -1; Texto = "";
        }
    }
}

