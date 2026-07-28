﻿using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axl.Base.Statics
{
    public static class ExcelHelper
    {
        private static Random _rng = new Random();

        private static readonly string[] _refinerias = {
            "REFINERIA LUJAN DE CUYO", "REFINERIA LA PLATA", "REFINERIA PLAZA HUINCUL",
            "REFINERIA CAMPANA", "REFINERIA BAHIA BLANCA", "REFINERIA SAN LORENZO"
        };

        private static readonly string[] _productos = {
            "GO G2", "GOM", "GHF", "GO G3", "JP", "NG2", "NG3", "BUTANO", "PROPANO", "GASOLINA", "KEROSENE"
        };

        private static readonly string[] _conceptos = {
            "STOCK INICIAL", "STOCK FINAL", "DELTA STOCK", "A TERMINAL",
            "A BUQUE", "A DUCTO", "X DEMANDA", "X VENTAS SPOT", "EXPORTACION"
        };

        public static void CreateChaosReport(string filePath)
        {
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

            using (var workbook = new XLWorkbook())
            {
                int sheetCount = _rng.Next(1, 4);
                for (int i = 1; i <= sheetCount; i++)
                {
                    var sheet = workbook.Worksheets.Add($"Zona_{i}_{_rng.Next(100, 999)}");
                    FillSheetWithChaos(sheet);
                }
                workbook.SaveAs(filePath);
            }
        }

        private static void FillSheetWithChaos(IXLWorksheet sheet)
        {
            int currentRow = 2;
            int tablesCount = _rng.Next(2, 5);

            for (int t = 0; t < tablesCount; t++)
            {
                string nombreRefineria = _refinerias[_rng.Next(_refinerias.Length)];
                int startCol = _rng.Next(1, 9);
                currentRow = GenerateTableBlock(sheet, currentRow, startCol, nombreRefineria);
                currentRow += _rng.Next(2, 6);
            }
            sheet.Columns().AdjustToContents();
        }

        private static int GenerateTableBlock(IXLWorksheet sheet, int startRow, int startCol, string title)
        {
            int r = startRow;
            int c = startCol;

            var titleCell = sheet.Cell(r, c);
            titleCell.Value = title;
            StyleHeader(titleCell, XLColor.DarkBlue);

            int numProducts = _rng.Next(4, 9);
            var misProductos = _productos.OrderBy(x => _rng.Next()).Take(numProducts).ToList();
            List<int> colOffsets = new List<int>();

            int currentOffset = 1;
            for (int i = 0; i < misProductos.Count; i++)
            {
                var cell = sheet.Cell(r, c + currentOffset);
                if (i < misProductos.Count - 1 && _rng.Next(100) < 20)
                {
                    var mergeRange = sheet.Range(r, c + currentOffset, r, c + currentOffset + 1);
                    mergeRange.Merge();
                    mergeRange.Value = misProductos[i] + "_HM";
                    StyleHeader(mergeRange.FirstCell(), XLColor.DarkRed);
                    colOffsets.Add(currentOffset);
                    colOffsets.Add(currentOffset + 1);
                    currentOffset += 2;
                }
                else
                {
                    cell.Value = misProductos[i];
                    StyleHeader(cell, XLColor.DarkBlue);
                    colOffsets.Add(currentOffset);
                    currentOffset++;
                }
            }

            r++;
            foreach (var concepto in _conceptos)
            {
                var labelCell = sheet.Cell(r, c);
                labelCell.Value = concepto;
                labelCell.Style.Font.Bold = true;
                labelCell.Style.Border.RightBorder = XLBorderStyleValues.Double;

                for (int j = 0; j < colOffsets.Count; j++)
                {
                    int colRelativa = colOffsets[j];
                    if (sheet.Cell(r, c + colRelativa).IsMerged()) continue;

                    if (j < colOffsets.Count - 1 && _rng.Next(100) < 15)
                    {
                        int colSiguiente = colOffsets[j + 1];
                        if (colSiguiente == colRelativa + 1)
                        {
                            var rangeData = sheet.Range(r, c + colRelativa, r, c + colSiguiente);
                            rangeData.Merge();
                            rangeData.Value = GenerateRandomValue();
                            rangeData.Style.Fill.BackgroundColor = XLColor.Yellow;
                            rangeData.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                        else WriteSingleValue(sheet, r, c + colRelativa);
                    }
                    else WriteSingleValue(sheet, r, c + colRelativa);
                }
                r++;
            }
            return r;
        }

        private static void WriteSingleValue(IXLWorksheet sheet, int r, int c)
        {
            var cell = sheet.Cell(r, c);
            cell.Value = GenerateRandomValue();
            cell.Style.NumberFormat.Format = "#,##0";
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        private static double GenerateRandomValue()
        {
            if (_rng.Next(100) < 30) return 0;
            return _rng.Next(-1000, 25000);
        }

        private static void StyleHeader(IXLCell cell, XLColor bg)
        {
            cell.Style.Fill.BackgroundColor = bg;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }
    }
}

