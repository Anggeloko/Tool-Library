# Tools.Omdb

Librería para la integración con la base de datos OM (Operational Management) de Foxboro mediante comandos de consola.

## Prerequisites
- **Foxboro Tools:** Debe estar instalado el conjunto de herramientas de Foxboro (ej. `omgetimp`, `omsetimp`, `omcrt`, `omfnd`).
- **Shell:** Requiere `ksh.exe` disponible en el sistema.
- **Ruta por defecto:** `d:\opt\fox\bin\tools`.

## Technical Reference (API)

### Clase `OmdbService`

#### Constructor `OmdbService(string workingDirectory = DefaultFoxboroPath)`
Inicializa el servicio verificando que la ruta de las herramientas exista.
- **Parámetros:**
  - `workingDirectory`: Ruta opcional a las herramientas. Si no se provee, usa la ruta estándar de Foxboro en el disco D.

#### `Write(string variable, object value, OmType type)`
Escribe un valor en la base de datos. Si la variable no existe, la crea automáticamente con el tipo especificado.
- **Tipos soportados (`OmType`):** `Bool`, `String`, `Float`, `Int`, `Long`.

#### `Read<T>(string variable, out bool exists)`
Lee el valor de una variable.
- **Retorno:** El valor convertido al tipo genérico `T`.
- **Salida:** `exists` indica si la variable fue encontrada.

#### `ReadBit(string variable, int bit, out bool exists)`
Lee un bit específico de una variable de tipo Packed Long (`pl`).
- **Parámetros:**
  - `variable`: Nombre de la variable PL.
  - `bit`: Índice del bit (0-31).

#### `CreateRawVariables(string prefix, int quantity, OmType type)`
Crea una secuencia de variables con formato `PREFIJO_001`, `PREFIJO_002`, etc.

## Ejemplo de Uso

```csharp
var service = new OmdbService(); // Usa d:\opt\fox\bin\tools por defecto

// Escribir un booleano
service.Write("MI_VAR_BOOL", true, OmType.Bool);

// Leer un float
float temp = service.Read<float>("TEMP_001", out bool exists);

if (exists) {
    Console.WriteLine($"Temperatura: {temp}");
}

---
*Versión: 1.0.2*
```
