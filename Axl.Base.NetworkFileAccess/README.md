# Tools.NetworkFileAccess

Library for secure access to network files and folders (UNC) using Windows user impersonation. Allows performing filesystem operations on shared network resources that require specific credentials.

## Features

- **User Impersonation**: Executes operations under the context of a specific network user.
- **Smart Copy/Move**: Automatically manages hybrid transfers between network and local paths, using an in-memory bridge to prevent permission conflicts.
- **UNC Validation**: Ensures network paths follow the standard `\\server\share` format.
- **Health Check**: Implements `ICheckable` to validate network share accessibility.
- **Compatibility**: Designed for .NET Framework 4.5.2+.

## Basic Usage

### Initialization

To use the service, a `UserSecurityContext` must first be created.

```csharp
using Tools.Security;
using Tools.NetworkFileAccess.Services;
using Tools.Statics;

// 1. Create security context
var securePass = WindowsHelper.ToSecureString("my_password");
var context = new UserSecurityContext("username", securePass, LogonType.NewCredentials, "domain");

// 2. Instantiate service
var networkService = new NetworkFileAccessService(context);
```

### File Operations

```csharp
// List files
string[] files = networkService.ListFiles(@"\\server\share\data");

// Write file (creates directories automatically if missing)
byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello Network");
networkService.WriteFile(@"\\server\share\logs\test.txt", data);

// Read file
byte[] readData = networkService.ReadFile(@"\\server\share\logs\test.txt");
```

### Smart Copy (Network <-> Local)

The library detects whether the source or destination is local and manages security context switching automatically:

```csharp
// Network to Local (Secure copy)
networkService.Copy(@"\\server\share\config.ini", @"C:\AppData\config.ini");

// Local to Network
networkService.Copy(@"C:\Logs\local.log", @"\\server\share\backups\local.log");
```

## Access Verification

You can verify whether a path is accessible before performing operations:

```csharp
var perms = networkService.TestAccess(@"\\server\share");
if (perms.HasFlag(DirectoryPermissions.Write)) {
    // We have write permissions
}

// Or via ICheckable
networkService.CheckPath = @"\\server\share";
var result = await networkService.CheckAsync();
```

## Internal Logic

### Context Management
When executing a copy between Network and Local paths, the library:
1. Enters the impersonated context to read/write on the network share.
2. Exits the impersonated context to read/write on the local drive.
This prevents the common "Access Denied" error that occurs when a network user attempts to write directly to the local C: drive.

---
*Version: 1.0.0*
*Dependency: Tools.Common 1.1.1*
