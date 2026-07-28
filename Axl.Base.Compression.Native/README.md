# Axl.Base.Compression.Native

Lightweight and secure compression library built solely on native .NET `System.IO.Compression`.

## Features
- **Zero External Dependencies**: Free of third-party vulnerabilities (does not use SharpCompress).
- **Target**: .NET Framework 4.5.2 or higher.
- **Security**: Fully compliant with strict corporate security policies.

## Basic Usage

### Dependency Injection
The library implements the common `ICompressionService` interface.

```csharp
ICompressionService compressor = new NativeCompressionService(log);
```

### Compression
Supports native Zip compression and emulation for other formats (Tar, 7z) via extension aliasing.

```csharp
var files = new[] { "data1.txt", "data2.txt" };
compressor.Compress(files, "archive.zip", CompressionFormat.Zip);

// Emulation for transactional purposes
compressor.Compress(files, "archive.7z", CompressionFormat.SevenZip);
```

### Decompression
Automatically detects format (Zip or GZip) for extraction.

```csharp
var extracted = compressor.Decompress("archive.zip", "C:\\Extracted");
```

## Supported Formats
| Format | Method | Notes |
| :--- | :--- | :--- |
| **Zip** | Native | Full support for multiple files. |
| **GZip** | Native | Recommended for single stream compression. |
| **7z / Tar / Tgz** | Emulated | Internal Zip structure with custom extension. |

## Limitations
- **Passwords**: Not natively supported by `System.IO.Compression`. The `password` parameter is ignored with a warning log.
- **Rar**: Not supported (requires third-party or proprietary libraries).
