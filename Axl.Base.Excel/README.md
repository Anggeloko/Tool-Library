# Tools.Excel

Specialized library for dynamic reading and manipulation of Microsoft Excel (.xlsx) files, optimized for complex reports with merged cells and variable formats.

## Dependencies
- **ClosedXML**: For complex reporting, formatting, and writing.
- **ExcelDataReader**: For fast and efficient reading of large data volumes.
- **Tools.Common**: Base models and interfaces.

## Technical Reference

### ExcelManager (Static)

The unified entry point for all read operations.

#### `ReadDynamicTables(string filename, List<ElShe> sheets_, ILog _log)`
Reads complex dynamic tables using `ClosedXML`. Supports 3 reading models:
- **Model 0 (Stacked)**: Tables stacked on top of each other with dynamic names.
- **Model 1/2 (Complex)**: Supports merged cells, "King of the hill" for titles, and indentation detection.
- **Model 3 (Standard)**: Reading simple tables with type recognition.

#### `ReadFast(string filename, string tname = "")`
High-performance alternative function using `ExcelDataReader`. Ideal for giant flat files.
- **Return**: `List<BaS>` (Base Sheet).

#### `GetSheets(string filename, ILog _log)`
Returns a list of names for all available sheets in the file.

---

### ExcelHelper (Static)

Provides utilities for report generation and testing.

#### `CreateChaosReport(string filePath)`
Generates a test Excel file with multiple tables, randomized merged cells, data type variations, and visual noise to validate the robustness of reading algorithms.

---

### Data Models

- **`BaS` (Base Sheet)**: Root container of an extracted table. Includes table name, sheet index, and column/value collections.
- **`BaC` (Base Column)**: Column definition featuring a **Type Voting System**. Automatically detects if a column is predominantly `string`, `double`, or `datetime`.
- **`ElShe`**: Input configuration to define which sheet to read and under which model.

## Usage Example (Fast Reading)

```csharp
using Tools.Statics;
using Tools.Models;

// Fast file reading
var results = ExcelManager.ReadFast("Report.xlsx");

foreach (var table in results) {
    Console.WriteLine($"Detected table: {table.Table}");
    foreach (var row in table.Values) {
        Console.WriteLine(string.Join(" | ", row));
    }
}
```

## Usage Example (Dynamic Extraction)

```csharp
var config = new List<ElShe> {
    new ElShe { Sheet = "Data", Model = 1 } // Complex model
};

var log = new FileLog("excel.log");
var tables = ExcelManager.ReadDynamicTables("Complex.xlsx", config, log);
```

---
*Version: 1.1.3*
