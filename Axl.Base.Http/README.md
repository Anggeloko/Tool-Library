# Tools.Http

High-performance HTTP client library designed for .NET Framework 4.5.2+ environments. Features automatic Bearer token management, connection pooling, native file upload support, and health checks.

## Features

- **Automatic Authentication**: Transparently manages token acquisition and renewal.
- **Dynamic Field Mapping**: Configurable custom keys for request payloads and authentication responses.
- **Connection Pooling**: Uses a shared `HttpClient` instance to prevent socket exhaustion.
- **Developer Mode**: Option to bypass SSL certificate validation during local testing.
- **File Management**: File upload support via `MultipartFormDataContent`.
- **Generic CRUD Engine**: Simplified methods for GET, POST, PUT, PATCH, and DELETE.

## Configuration

The library is configured using the `HttpConfig` class:

```csharp
var config = new HttpConfig {
    BaseUrl = "https://api.yoursite.com/",
    AuthEndpoint = "/api/auth/login",
    Username = "your_username",
    Password = "your_password",
    
    // Dynamic Mapping (Request)
    UserField = "username",      // Default: "username"
    PasswordField = "password",  // Default: "password"
    
    // Dynamic Mapping (Response)
    TokenKey = "access_token",   // Default: "access_token"
    ExpirationKey = "expires_in", // Default: "expires_in"
    
    // Advanced Options
    TimeoutSeconds = 300,        // Global request timeout
    UseAuth = true,              // Indicates if Bearer token should be attached
    UserAgent = "MyApp/1.0",     // Custom User-Agent
    IsDeveloperMode = false      // Bypasses SSL errors on localhost
};
```

## Basic Usage

### Initialization

```csharp
// Inject a logger (optional)
var logger = new FileLog(new NewtonJson());
var http = new HttpService(config, logger);
```

### Connectivity Test
Quickly verifies if the server is reachable:

```csharp
var health = await http.CheckAsync();
if (health.IsSuccess && health.Value) {
    // Server responded successfully
}
```

### Generic CRUD Operations

```csharp
// GET
var result = await http.GetAsync<MyModel>("api/data/1");

// POST
var result = await http.PostAsync<MyReq, MyRes>("api/data", payload);

// PUT / PATCH
await http.PutAsync<MyReq, object>("api/data/1", updatePayload);
await http.PatchAsync<MyReq, object>("api/data/1", partialPayload);

// DELETE
var deleted = await http.DeleteAsync("api/data/1");
```

## File Uploads

### Native Upload (Multipart)
Recommended for large files. Uses `MultipartFormDataContent` internally.

```csharp
var result = await http.UploadFileAsync("C:\\temp\\file.pdf", "api/upload");
```

### Base64 Upload (Custom Payload Structure)
Useful when the target API expects embedded JSON file payloads:

```csharp
var uploadPayload = new {
    fileName = "document.pdf",
    base64Content = Convert.ToBase64String(File.ReadAllBytes("path/to/file.pdf"))
};

var result = await http.PostAsync<object, object>("api/upload/base64", uploadPayload);
```

## Technical Details

### Token Expiration Handling
The service handles the `ExpirationKey` field in multiple format variations:
1. **Relative Seconds**: (e.g. `3600`) Expires in N seconds from current time.
2. **Unix Timestamp (s/ms)**: (e.g. `1714856400`) Universal epoch timestamp.
3. **ISO Date**: (e.g. `"2024-05-04T20:00:00Z"`) Absolute date.

### Developer Mode
When `IsDeveloperMode` is `true`, the library bypasses SSL certificate validation, simplifying development with local APIs or self-signed certificates.

---
*Version: 1.1.7*
