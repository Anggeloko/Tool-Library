# Axl.Base.Persistence

Librería para la persistencia robusta de variables y configuraciones locales. Proporciona un mecanismo para guardar objetos serializados en archivos `.bin` con encriptación hardware-linked y compresión GZip.

## Características Principales

- **Seguridad**: Encriptación AES-256 utilizando un hash derivado del hardware de la máquina (`MachineInfo`).
- **Eficiencia**: Compresión automática mediante `GZipStream`.
- **Mantenimiento**: Limpieza automática de archivos antiguos opcional.
- **Abstracción**: Interfaz `IVariableStorage` para desacoplar la lógica de negocio del almacenamiento físico.

## Instalación

Añadir la referencia al proyecto o instalar vía NuGet:

```xml
<ProjectReference Include="..\Axl.Base.Persistence\Axl.Base.Persistence.csproj" />
```

## Uso Básico

### Inicialización

Se recomienda inyectar el servicio de persistencia pasando la ruta base, un proveedor de JSON (como `LegacyJson`) y un Logger.

```csharp
using Axl.Base.Persistence.Interfaces;
using Axl.Base.Persistence.Services;
using Axl.Base.Json.Legacy;
using Axl.Base.Logs;

// ...
string basePath = @"C:\ProgramData\MyApp\Config";
IVariableStorage storage = new LocalVariableStorage(basePath, new LegacyJson(), new FileLog());
```

### Guardar una Variable

```csharp
var myConfig = new { Server = "127.0.0.1", Port = 8080 };
storage.Save("CONNECTION_SETTINGS", myConfig);
```

### Cargar una Variable

```csharp
var config = storage.Load<MyConfigClass>("CONNECTION_SETTINGS");
if (config != null) 
{
    // Usar la configuración...
}
```

### Eliminar una Variable

```csharp
storage.Delete("CONNECTION_SETTINGS");
```

#### `bool UseCompression`
Define si los archivos se guardan comprimidos con GZip. Por defecto es `true`.
- **Nota:** Si se desactiva, los archivos nuevos serán texto plano encriptado, pero la librería seguirá intentando descomprimir archivos existentes si detecta el formato GZip.

## Configuración de Limpieza (FileCleaner)

El constructor acepta un parámetro opcional `cleanupDays` (por defecto `1`):

- **Si es > 0**: Borra automáticamente los archivos en el directorio de datos que tengan más días de antigüedad que el valor especificado.
- **Si es <= 0**: Deshabilita la limpieza automática.

```csharp
// Deshabilitar limpieza automática
var storage = new LocalVariableStorage(path, json, log, cleanupDays: 0);
```

## Dependencias

- `Axl.Base.Common`: Para encriptación, hashes de hardware y utilidades de limpieza.
- `Axl.Base.Interfaces`: Para `IJson` e `ILog`.
