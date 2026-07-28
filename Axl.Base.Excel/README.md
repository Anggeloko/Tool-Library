# Tools.Excel

Biblioteca especializada en la manipulación y lectura dinámica de archivos Microsoft Excel (.xlsx), optimizada para reportes complejos con celdas combinadas y formatos variables.

## Dependencias
- **ClosedXML**: Para manejo de reportes complejos, formateo y escritura.
- **ExcelDataReader**: Para lectura rápida y eficiente de grandes volúmenes de datos.
- **Tools.Common**: Modelos base e interfaces.

## Referencia Técnica

### ExcelManager (Estático)

Es el punto de entrada unificado para todas las operaciones de lectura.

#### `ReadDynamicTables(string filename, List<ElShe> sheets_, ILog _log)`
Lee tablas dinámicas complejas utilizando `ClosedXML`. Soporta 3 modelos de lectura:
- **Modelo 0 (Stacked)**: Tablas apiladas una debajo de otra con nombres dinámicos.
- **Modelo 1/2 (Complex)**: Soporta celdas combinadas, "King of the hill" para títulos y detección de indentación.
- **Modelo 3 (Standard)**: Lectura de tablas simples con reconocimiento de tipos.

#### `ReadFast(string filename, string tname = "")`
Función alternativa de alto rendimiento que utiliza `ExcelDataReader`. Ideal para archivos planos gigantes.
- **Retorno**: `List<BaS>` (Base Sheet).

#### `GetSheets(string filename, ILog _log)`
Retorna una lista con los nombres de todas las hojas disponibles en el archivo.

---

### ExcelHelper (Estático)

Provee utilidades para generación de reportes y pruebas.

#### `CreateChaosReport(string filePath)`
Genera un archivo Excel de prueba con múltiples tablas, celdas combinadas aleatorias, variaciones de tipos de datos y ruido visual para validar la robustez de los algoritmos de lectura.

---

### Models de Datos

- **`BaS` (Base Sheet)**: Contenedor raíz de una tabla extraída. Incluye el nombre de la tabla, índice de hoja y colecciones de columnas/valores.
- **`BaC` (Base Column)**: Definición de columna con sistema de **Votación de Tipos**. Detecta automáticamente si una columna es predominantemente `string`, `double` o `datetime`.
- **`ElShe`**: Configuración de entrada para definir qué hoja leer y bajo qué modelo.

## Ejemplo de Uso (Lectura Rápida)

```csharp
using Tools.Statics;
using Tools.Models;

// Lectura rápida de un archivo
var results = ExcelManager.ReadFast("Reporte.xlsx");

foreach (var table in results) {
    Console.WriteLine($"Tabla detectada: {table.Table}");
    foreach (var row in table.Values) {
        Console.WriteLine(string.Join(" | ", row));
    }
}
```

## Ejemplo de Uso (Extracción Dinámica)

```csharp
var config = new List<ElShe> {
    new ElShe { Sheet = "Datos", Model = 1 } // Modelo complejo
};

var log = new FileLog("excel.log");
var tables = ExcelManager.ReadDynamicTables("Complejo.xlsx", config, log);

---
*Versión: 1.1.3*
```
