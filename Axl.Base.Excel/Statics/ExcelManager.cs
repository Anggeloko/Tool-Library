﻿using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Statics
{
    public static class ExcelManager
    {
        #region --- Lectura Avanzada (ClosedXML) ---

        public static List<string> GetSheets(string filename, ILog _log)
        {
            List<string> sheets = new List<string>();
            if (!File.Exists(filename)) return sheets;

            try
            {
                using (var workbook = new XLWorkbook(filename))
                {
                    foreach (var sheet in workbook.Worksheets)
                    {
                        sheets.Add(sheet.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ExceptionUtils.Format(ex, "GetSheets"));
            }
            return sheets;
        }

        public static List<BaS> ReadDynamicTables(string filename, List<ElShe> sheets_, ILog _log)
        {
            List<BaS> r = new List<BaS>();
            if (!File.Exists(filename)) return r;
            if (sheets_ == null || !sheets_.Any()) return r;

            string cleanFileName = Regex.Replace(Path.GetFileNameWithoutExtension(filename), @"[^0-9a-zA-Z\s]", "").Trim().Replace(" ", "_").ToUpper();

            try
            {
                using (var workbook = new XLWorkbook(filename))
                {
                    foreach (var configSheet in sheets_)
                    {
                        if (workbook.TryGetWorksheet(configSheet.Sheet, out var sheet))
                        {
                            List<BaS> result = new List<BaS>();
                            try
                            {
                                switch (configSheet.Model)
                                {
                                    case 0:
                                        result = StackedTables(sheet, cleanFileName, sheet.Position);
                                        break;
                                    case 1:
                                    case 2:
                                        result = ComplexTables(sheet, cleanFileName, sheet.Position);
                                        break;
                                    case 3:
                                        result = StandardTable(sheet, cleanFileName, sheet.Position);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.Error($"Error in sheet {configSheet.Sheet}: " + string.Join(" | ", ExceptionUtils.Format(ex)));
                            }

                            if (result != null && result.Count > 0)
                                r.AddRange(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ExceptionUtils.Format(ex, "ReadDynamicTables"));
            }
            return r;
        }

        #endregion

        #region --- Lectura Rápida (ExcelDataReader) ---

        /// <summary>
        /// Función alternativa para lectura rápida de hojas completas.
        /// </summary>
        public static List<BaS> ReadFast(string filename, string tname = "")
        {
            char[] ab = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
            int adx = 0;
            List<BaS> bas = new List<BaS>();

            if (!File.Exists(filename)) return bas;

            tname = string.IsNullOrEmpty(tname) ? Regex.Replace(Path.GetFileNameWithoutExtension(filename), @"[^0-9a-zA-Z\s]", "").Replace(" ", "_").ToUpper() : tname;

            using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                using (IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                {
                    DataSet result = excelReader.AsDataSet();
                    foreach (DataTable table in result.Tables)
                    {
                        string tn = result.Tables.Count >= 2 ? $"{tname}_{Regex.Replace(table.TableName, @"[^0-9a-zA-Z\s]", "").Replace(" ", "_").ToUpper()}" : tname;
                        BaS b = new BaS() { Table = tn };
                        int rdx = 0;
                        foreach (DataRow row in table.Rows)
                        {
                            int cdx = 0;
                            List<string> l = new List<string>();
                            foreach (DataColumn column in table.Columns)
                            {
                                if (rdx == 0)
                                {
                                    string valStr = Regex.Replace(row[column].ToString(), @"[^0-9a-zA-Z\s]", "").Trim().Replace(" ", "_").ToLower();
                                    if (string.IsNullOrEmpty(valStr)) valStr = $"col_{cdx}";

                                    while (b.Columns.Any(x => x.Column == valStr))
                                    {
                                        valStr += ab[adx % ab.Length];
                                        adx++;
                                    }
                                    b.Columns.Add(new BaC() { Column = valStr, Index = cdx, Voting = new Dictionary<string, int>() { { column.DataType.Name.ToLower(), 1 } } });
                                }
                                else if (cdx < b.Columns.Count)
                                {
                                    string t = row[column].GetType().Name.ToLower();
                                    if (b.Columns[cdx].Voting.ContainsKey(t)) b.Columns[cdx].Voting[t]++;
                                    else b.Columns[cdx].Voting.Add(t, 1);
                                    l.Add(b.Columns[cdx].DataValue(row[column]).Trim());
                                }
                                cdx++;
                            }
                            if (l.Count > 0) b.Values.Add(l);
                            rdx++;
                        }
                        bas.Add(b);
                    }
                }
            }
            return bas;
        }

        #endregion

        #region --- Métodos Privados de Lógica (ClosedXML) ---

        private static List<BaS> StackedTables(IXLWorksheet sheet, string cleanFileName, int sheetIdx)
        {
            var tables = new List<BaS>();
            UnmergeAndExpand(sheet);

            var rows = sheet.RowsUsed();
            if (!rows.Any()) return tables;

            int firstRow = rows.First().RowNumber();
            int lastRow = rows.Last().RowNumber();

            BaS currentTable = null;
            int tableCounterInSheet = 1;

            for (int r = firstRow; r <= lastRow; r++)
            {
                var row = sheet.Row(r);
                bool isRowEmpty = row.IsEmpty();

                if (!isRowEmpty && currentTable == null)
                {
                    var cellsUsed = row.CellsUsed().Count();
                    if (cellsUsed == 1 && r < lastRow && sheet.Row(r + 1).CellsUsed().Count() > 1) continue;
                }

                if (currentTable == null)
                {
                    if (!isRowEmpty)
                    {
                        currentTable = new BaS();
                        var firstCell = row.CellsUsed().FirstOrDefault();
                        string cleanTableName = firstCell != null ? Regex.Replace(firstCell.GetString(), @"[^0-9a-zA-Z\s]", "").Trim().Replace(" ", "_").ToUpper() : "TBL";
                        currentTable.Sheet = sheetIdx;
                        currentTable.Table = $"{cleanFileName}_{sheet.Name}_{tableCounterInSheet}_{cleanTableName}";
                        tableCounterInSheet++;
                        ProcessHeaders(row, currentTable);
                    }
                }
                else
                {
                    if (isRowEmpty)
                    {
                        if (currentTable.Values.Count > 0) tables.Add(currentTable);
                        currentTable = null;
                    }
                    else
                    {
                        ProcessDataRow(row, currentTable);
                    }
                }
            }
            if (currentTable != null && currentTable.Values.Count > 0) tables.Add(currentTable);
            return tables;
        }

        private static List<BaS> ComplexTables(IXLWorksheet sheet, string cleanFileName, int sheetIdx)
        {
            var tables = new List<BaS>();
            UnmergeAndExpand(sheet);

            var rowsUsed = sheet.RowsUsed();
            if (!rowsUsed.Any()) return tables;

            int firstRow = rowsUsed.First().RowNumber();
            int lastRow = rowsUsed.Last().RowNumber();

            BaS currentTable = null;
            int tableCounter = 1;
            string candidateTitle = "CONCEPTO";

            for (int r = firstRow; r <= lastRow; r++)
            {
                var row = sheet.Row(r);
                bool isRowEmpty = row.IsEmpty();

                if (currentTable == null && !isRowEmpty)
                {
                    var cells = row.CellsUsed().ToList();
                    int uniqueValues = cells.Select(c => c.GetString().Trim()).Distinct().Count();

                    if (uniqueValues == 1 || cells.Count == 1)
                    {
                        string text = cells.First().GetString().Trim();
                        if (!string.IsNullOrEmpty(text) && (!double.TryParse(text, out double num) || (num > 1900 && num < 2100)))
                        {
                            candidateTitle = Regex.Replace(text, @"[^0-9a-zA-Z\s]", "").Trim().Replace(" ", "_").ToUpper();
                        }
                        continue;
                    }
                }

                if (currentTable == null)
                {
                    if (!isRowEmpty)
                    {
                        currentTable = new BaS { Sheet = sheetIdx };
                        currentTable.Table = $"{cleanFileName}_{sheet.Name}_{tableCounter}_{candidateTitle}";
                        ProcessHeadersComplex(row, currentTable, candidateTitle);
                        tableCounter++;
                        candidateTitle = "CONCEPTO";
                    }
                }
                else
                {
                    if (isRowEmpty)
                    {
                        bool closeTable = true;
                        if (r < lastRow && !sheet.Row(r + 1).IsEmpty()) closeTable = false;

                        if (closeTable)
                        {
                            if (currentTable.Values.Count > 0) tables.Add(currentTable);
                            currentTable = null;
                        }
                    }
                    else
                    {
                        ProcessDataRowComplex(row, currentTable);
                    }
                }
            }
            if (currentTable != null && currentTable.Values.Count > 0) tables.Add(currentTable);
            return tables;
        }

        private static List<BaS> StandardTable(IXLWorksheet sheet, string cleanFileName, int sheetIdx)
        {
            var tables = new List<BaS>();
            UnmergeAndExpand(sheet);

            var rowsUsed = sheet.RowsUsed();
            if (!rowsUsed.Any()) return tables;

            var headerRow = rowsUsed.First();
            int firstRowIdx = headerRow.RowNumber();
            int lastRowIdx = rowsUsed.Last().RowNumber();

            var currentTable = new BaS { Sheet = sheetIdx, Table = $"{cleanFileName}_{sheet.Name}_MAIN" };
            int lastColIndex = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            for (int colIdx = 1; colIdx <= lastColIndex; colIdx++)
            {
                string cleanName = Regex.Replace(headerRow.Cell(colIdx).GetString(), @"[^0-9a-zA-Z\s]", "").Trim().Replace(" ", "_").ToUpper();
                if (string.IsNullOrWhiteSpace(cleanName)) cleanName = $"COL_{colIdx}";
                if (currentTable.Columns.Any(c => c.Column == cleanName)) cleanName = $"{cleanName}_{colIdx}";

                currentTable.Columns.Add(new BaC { Column = cleanName, Index = colIdx, Voting = new Dictionary<string, int> { { "string", 0 } } });
            }

            for (int r = firstRowIdx + 1; r <= lastRowIdx; r++)
            {
                var row = sheet.Row(r);
                if (!row.IsEmpty()) ProcessDataRowStandard(row, currentTable);
            }

            if (currentTable.Values.Count > 0) tables.Add(currentTable);
            return tables;
        }

        private static void ProcessHeaders(IXLRow row, BaS table)
        {
            foreach (var cell in row.CellsUsed())
            {
                string cleanName = Regex.Replace(cell.GetString(), @"[^0-9a-zA-Z\s]", "").Trim().Replace(" ", "_").ToUpper();
                if (string.IsNullOrWhiteSpace(cleanName)) continue;
                string originalName = cleanName;
                int counter = 1;
                while (table.Columns.Any(c => c.Column == cleanName)) cleanName = $"{originalName}_{counter++}";
                table.Columns.Add(new BaC { Column = cleanName, Index = cell.Address.ColumnNumber, Voting = new Dictionary<string, int> { { "string", 0 } } });
            }
        }

        private static void ProcessDataRow(IXLRow row, BaS table)
        {
            List<string> rowValues = new List<string>();
            bool hasData = false;
            foreach (var colDef in table.Columns)
            {
                var cell = row.Cell(colDef.Index);
                object valObj = DBNull.Value;
                string typeName = "string";

                if (!cell.IsEmpty())
                {
                    switch (cell.DataType)
                    {
                        case XLDataType.Number: valObj = cell.GetDouble(); typeName = "double"; break;
                        case XLDataType.DateTime: valObj = cell.GetDateTime(); typeName = "datetime"; break;
                        case XLDataType.Boolean: valObj = cell.GetBoolean(); typeName = "bool"; break;
                        default:
                            string sVal = cell.GetString();
                            if (!string.IsNullOrWhiteSpace(sVal)) { valObj = sVal; typeName = "string"; }
                            break;
                    }
                    if (valObj != DBNull.Value) hasData = true;
                }

                if (valObj != DBNull.Value)
                {
                    if (colDef.Voting.ContainsKey(typeName)) colDef.Voting[typeName]++;
                    else colDef.Voting.Add(typeName, 1);
                }
                rowValues.Add(colDef.DataValue(valObj));
            }
            if (hasData) table.Values.Add(rowValues);
        }

        private static void UnmergeAndExpand(IXLWorksheet sheet)
        {
            var mergedRanges = sheet.MergedRanges.ToList();
            foreach (var range in mergedRanges)
            {
                var value = range.FirstCell().Value;
                sheet.MergedRanges.Remove(range);
                foreach (var cell in range.Cells()) cell.Value = value;
            }
        }

        private static void ProcessHeadersComplex(IXLRow row, BaS table, string titleForEmptyCol)
        {
            int lastCol = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (int i = 1; i <= lastCol; i++)
            {
                string clean = Regex.Replace(row.Cell(i).GetString(), @"[^0-9a-zA-Z\s]", "").Trim().Replace(" ", "_").ToUpper();
                if (i == 1 && string.IsNullOrWhiteSpace(clean)) clean = titleForEmptyCol;

                bool isNumericHeader = double.TryParse(clean.Replace("_", ""), out double d);
                if (string.IsNullOrWhiteSpace(clean) || (isNumericHeader && i > 1)) continue;

                string original = clean;
                int c = 1;
                while (table.Columns.Any(x => x.Column == clean)) clean = $"{original}_{c++}";

                table.Columns.Add(new BaC { Column = clean, Index = i, Voting = new Dictionary<string, int> { { "string", 0 } } });
            }
        }

        private static void ProcessDataRowComplex(IXLRow row, BaS table)
        {
            List<string> rowValues = new List<string>();
            bool hasData = false;
            foreach (var col in table.Columns)
            {
                var cell = row.Cell(col.Index);
                string textVal = cell.IsEmpty() ? "" : cell.Value.ToString().Trim();

                if (col == table.Columns.First() && string.IsNullOrEmpty(textVal))
                {
                    var neighbor = row.Cell(col.Index + 1);
                    if (!neighbor.IsEmpty()) textVal = neighbor.Value.ToString().Trim();
                }

                object valObj = DBNull.Value;
                string typeName = "string";

                if (!string.IsNullOrEmpty(textVal))
                {
                    if (textVal == "-") valObj = DBNull.Value;
                    else if (col == table.Columns.First()) { valObj = textVal; typeName = "string"; }
                    else
                    {
                        switch (cell.DataType)
                        {
                            case XLDataType.Number: valObj = cell.GetDouble(); typeName = "double"; break;
                            case XLDataType.DateTime: valObj = cell.GetDateTime(); typeName = "datetime"; break;
                            default: valObj = textVal; typeName = "string"; break;
                        }
                    }
                }

                if (valObj != DBNull.Value)
                {
                    hasData = true;
                    int weight = (col == table.Columns.First()) ? 100 : 1;
                    if (col.Voting.ContainsKey(typeName)) col.Voting[typeName] += weight;
                    else col.Voting.Add(typeName, weight);
                }
                rowValues.Add(col.DataValue(valObj));
            }
            if (hasData) table.Values.Add(rowValues);
        }

        private static void ProcessDataRowStandard(IXLRow row, BaS table)
        {
            List<string> rowValues = new List<string>();
            bool hasData = false;
            foreach (var col in table.Columns)
            {
                var cell = row.Cell(col.Index);
                object valObj = DBNull.Value;
                string typeName = "string";

                if (!cell.IsEmpty())
                {
                    switch (cell.DataType)
                    {
                        case XLDataType.Number: valObj = cell.GetDouble(); typeName = "double"; break;
                        case XLDataType.DateTime: valObj = cell.GetDateTime(); typeName = "datetime"; break;
                        case XLDataType.Boolean: valObj = cell.GetBoolean(); typeName = "bool"; break;
                        default:
                            string textVal = cell.GetString().Trim();
                            if (!string.IsNullOrEmpty(textVal)) { valObj = textVal; typeName = "string"; }
                            break;
                    }
                }

                if (valObj != DBNull.Value)
                {
                    hasData = true;
                    if (col.Voting.ContainsKey(typeName)) col.Voting[typeName]++;
                    else col.Voting.Add(typeName, 1);
                }
                rowValues.Add(col.DataValue(valObj));
            }
            if (hasData) table.Values.Add(rowValues);
        }

        #endregion
    }
}

