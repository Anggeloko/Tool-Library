﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axl.Base.Models
{
    public class Performance
    {
        public List<Prc> Processor { get; set; }
        public List<RAM> MemRAM { get; set; }
        public List<HDD> MemHDD { get; set; }
        public Performance()
        {
            Processor = new List<Prc>();
            MemRAM = new List<RAM>();
            MemHDD = new List<HDD>();
        }
        public void AddPRC(List<Dictionary<string, string>> data)
        {
            if (data.Count > 0)
            {
                Prc total = new Prc
                {
                    Core = "avg",
                    Load = 0
                };
                for (int j = 0; j < data.Count; j++)
                {
                    Prc tempo = new Prc
                    {
                        Core = data[j]["id"],
                        Load = double.TryParse(data[j]["carga"], out double res) ? res : 0
                        //Load = 0.95
                    };
                    total.Load += tempo.Load;
                    Processor.Add(tempo);
                }
                total.Load = data.Count != 0 ? total.Load / data.Count : 0;
                Processor.Add(total);
            }
        }
        public void AddRAM(List<Dictionary<string, string>> data)
        {
            if (data.Count > 0)
            {
                for (int j = 0; j < data.Count; j++)
                {
                    RAM tempo = new RAM
                    {
                        Installed = double.TryParse(data[j]["instalada"], out double res) ? res : 0,
                        Free = double.TryParse(data[j]["libre"], out res) ? res : 0
                    };
                    tempo.CalcUsed();
                    MemRAM.Add(tempo);
                }
            }
        }
        public void AddHDD(List<Dictionary<string, string>> data)
        {
            if (data.Count > 0)
            {
                HDD total = new HDD
                {
                    Name = "avg",
                    Size = 0,
                    Free = 0
                };
                for (int j = 0; j < data.Count; j++)
                {
                    HDD tempo = new HDD
                    {
                        Name = data[j]["nombre"],
                        Size = double.TryParse(data[j]["capacidad"], out double res) ? res : 0,
                        Free = double.TryParse(data[j]["libre"], out res) ? res : 0
                    };
                    total.Size += tempo.Size;
                    total.Free += tempo.Free;
                    tempo.CalcUsed();
                    MemHDD.Add(tempo);
                }
                total.CalcUsed();
                MemHDD.Add(total);
            }
        }
    }
    public class Prc
    {
        public string Core { get; set; }
        public double Load { get; set; }
        public double LoadPrc { get { return Load / 100; } }
    }
    public class HDD
    {
        public string Name { get; set; }
        public double Size { get; set; }
        public double Free { get; set; }
        public double Used { get; set; }
        public void CalcUsed()
        {
            if (Size != 0)
                Used = (Size - Free) / Size;
            else
                Used = 0;
        }
    }
    public class RAM
    {
        public double Installed { get; set; }
        public double Free { get; set; }
        public double Used { get; set; }
        public void CalcUsed()
        {
            if (Installed != 0)
                Used = (Installed - Free) / Installed;
            else
                Used = 0;
        }
    }

}

