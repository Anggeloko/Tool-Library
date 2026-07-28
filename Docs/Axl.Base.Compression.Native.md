# Axl.Base.Compression.Native

Librería de compresión ligera y segura basada únicamente en `System.IO.Compression` nativo de .NET.

## Características
- **Cero Dependencias Externas**: Sin vulnerabilidades de terceros (no usa SharpCompress).
- **Target**: .NET Framework 4.5.2 o superior.
- **Seguridad**: Totalmente compatible con políticas de seguridad estrictas.

## Uso Básico

### Inyección de Dependencia
La librería implementa la interfaz común `ICompressionService`.

```csharp
ICompressionService compressor = new NativeCompressionService(log);
```

### Compresión
Soporta compresión Zip nativa y emulación de otros formatos (Tar, 7z) mediante el cambio de extensión.

```csharp
var files = new[] { "data1.txt", "data2.txt" };
compressor.Compress(files, "archive.zip", CompressionFormat.Zip);

// Emulación para fines transaccionales
compressor.Compress(files, "archive.7z", CompressionFormat.SevenZip);
```

### Descompresión
Detecta automáticamente el formato (Zip o GZip) para la extracción.

```csharp
var extracted = compressor.Decompress("archive.zip", "C:\\Extracted");
```

## Formatos Soportados
| Formato | Método | Notas |
| :--- | :--- | :--- |
| **Zip** | Nativo | Soporte completo para múltiples archivos. |
| **GZip** | Nativo | Recomendado para compresión de flujo único. |
| **7z / Tar / Tgz** | Emulado | Estructura interna Zip con extensión personalizada. |

## Limitaciones
- **Passwords**: No soportado nativamente por `System.IO.Compression`. El parámetro `password` es ignorado con un log de advertencia.
- **Rar**: No soportado (requiere librerías propietarias o de terceros).
