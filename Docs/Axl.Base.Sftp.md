# Tools.Sftp

Librería de cliente SFTP robusta para .NET Framework 4.5.2+, basada en SSH.NET. Proporciona métodos simplificados para la gestión recursiva de directorios y transferencia de archivos.

## Características

- **Operaciones Recursivas**: Sube y descarga estructuras de carpetas completas automáticamente.
- **Normalización de Rutas**: Gestiona separadores de ruta (`\` vs `/`) de forma transparente entre Windows y Linux.
- **Inyección de Dependencias**: Diseñada para trabajar con `ISftpService` e `ILog`.
- **Compatibilidad Legacy**: Optimizada para .NET 4.5.2.

## Configuración

```csharp
var config = new SftpConfig {
    Host = "127.0.0.1",
    Port = 22,
    User = "admin",
    Password = "password_seguro",
    RemotePath = "/uploads" // Ruta base para todas las operaciones
};
```

## Uso Básico

### Subir un Archivo

```csharp
var sftp = new SftpService(config, logger);
var result = await sftp.UploadFile("ruta/local/archivo.txt", "carpeta/remota");
```

### Subir una Carpeta (Recursivo)

```csharp
// Sube todo el contenido de la carpeta local al destino remoto
var result = await sftp.UploadFolder("C:\\MisDatos", "respaldo/hoy");
```

### Descargar una Carpeta

```csharp
var result = await sftp.DownloadFolder("remoto/datos", "C:\\Descargas");
```

## Lógica Interna

### Manejo de Rutas
La librería normaliza las rutas automáticamente. Si usas `C:\Temp` como ruta local y `carpeta/sub` como remota, el servicio asegura que el servidor SFTP reciba `/carpeta/sub` con slashes estilo Linux.

### Creación de Directorios
Al subir archivos o carpetas, el servicio verifica automáticamente si los directorios de destino existen y los crea recursivamente si es necesario.

## Manejo de Errores
Retorna `Result<T>` o `Result<List<string>>` para operaciones en lote, proporcionando una lista detallada de cualquier archivo que haya fallado durante las transferencias recursivas.

---
*Versión: 1.1.1*
*Dependencia: SSH.NET 2020.0.2*
