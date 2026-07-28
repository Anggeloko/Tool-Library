﻿using System;
using System.Collections.Generic;

namespace Axl.Base.Models
{
    public class Printer
    {
        public List<Supply> Supplies { get; set; }

        public Printer()
        {
            Supplies = new List<Supply>();
        }

        public void AddSupplies(List<Dictionary<string, string>> data)
        {
            foreach (var item in data)
            {
                var supply = new Supply
                {
                    Name = item.ContainsKey("nombre") ? item["nombre"] : "Unknown",
                    MaxCapacity = double.TryParse(item.ContainsKey("capacidad") ? item["capacidad"] : "0", out var cap) ? cap : 0,
                    Level = double.TryParse(item.ContainsKey("nivel") ? item["nivel"] : "0", out var lvl) ? lvl : 0
                };
                supply.CalcPercentage();
                Supplies.Add(supply);
            }
        }
    }

    public class Supply
    {
        public string Name { get; set; }
        public double MaxCapacity { get; set; }
        public double Level { get; set; }
        public double RemainingPercentage { get; set; }

        public void CalcPercentage()
        {
            if (MaxCapacity > 0 && Level >= 0)
                RemainingPercentage = Level / MaxCapacity;
            else if (MaxCapacity <= 0)
                RemainingPercentage = 0;
            
            // Niveles negativos en SNMP suelen indicar alertas u otros estatus (ej. -3 poco, -2 desconocido).
            if (Level < 0) RemainingPercentage = 0;
        }
    }
}

