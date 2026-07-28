# Tools.Sftp

Robust SFTP client library for .NET Framework 4.5.2+, based on SSH.NET. Provides simplified methods for recursive directory management and file transfers.

## Features

- **Recursive Operations**: Automatically uploads and downloads entire folder structures.
- **Path Normalization**: Transparently handles path separators (`\` vs `/`) between Windows and Linux systems.
- **Dependency Injection**: Designed to work seamlessly with `ISftpService` and `ILog`.
- **Legacy Compatibility**: Optimized for .NET 4.5.2.

## Configuration

```csharp
var config = new SftpConfig {
    Host = "127.0.0.1",
    Port = 22,
    User = "admin",
    Password = "secure_password",
    RemotePath = "/uploads" // Base path for all operations
};
```

## Basic Usage

### Uploading a File

```csharp
var sftp = new SftpService(config, logger);
var result = await sftp.UploadFile("local/path/file.txt", "remote/folder");
```

### Uploading a Folder (Recursive)

```csharp
// Uploads all contents of local folder to remote destination
var result = await sftp.UploadFolder("C:\\MyData", "backup/today");
```

### Downloading a Folder

```csharp
var result = await sftp.DownloadFolder("remote/data", "C:\\Downloads");
```

## Internal Logic

### Path Handling
The library normalizes paths automatically. If you supply `C:\Temp` as local path and `folder/sub` as remote, the service ensures the SFTP server receives Linux-style slashes `/folder/sub`.

### Directory Creation
When uploading files or folders, the service automatically checks if destination directories exist and creates them recursively if necessary.

## Error Handling
Returns `Result<T>` or `Result<List<string>>` for batch operations, providing a detailed list of any files that failed during recursive transfers.

---
*Version: 1.1.1*
*Dependency: SSH.NET 2020.0.2*
