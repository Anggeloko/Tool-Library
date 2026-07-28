# Tools.Http

Librería de cliente HTTP de alto rendimiento diseñada para entornos .NET Framework 4.5.2+. Incluye gestión automática de tokens Bearer, pooling de conexiones y soporte nativo para subida de archivos y comprobación de salud.

## Características

- **Autenticación Automática**: Gestiona la obtención y refresco de tokens de forma transparente.
- **Mapeo Dinámico de Campos**: Configura llaves personalizadas para la petición y respuesta de autenticación.
- **Pooling de Conexiones**: Utiliza una instancia compartida de `HttpClient` para prevenir el agotamiento de sockets.
- **Modo Desarrollador**: Opción para omitir la validación de certificados SSL en pruebas locales.
- **Gestión de Archivos**: Soporte para subida de archivos mediante `MultipartFormDataContent`.
- **Motor CRUD Genérico**: Métodos simplificados para GET, POST, PUT, PATCH y DELETE.

## Configuración

La librería se configura mediante la clase `HttpConfig`:

```csharp
var config = new HttpConfig {
    BaseUrl = "https://api.tusitio.com/",
    AuthEndpoint = "/api/auth/login",
    Username = "tu_usuario",
    Password = "tu_password",
    
    // Mapeo Dinámico (Petición)
    UserField = "username",      // Por defecto: "username"
    PasswordField = "password",  // Por defecto: "password"
    
    // Mapeo Dinámico (Respuesta)
    TokenKey = "access_token",   // Por defecto: "access_token"
    ExpirationKey = "expires_in", // Por defecto: "expires_in"
    
    // Opciones Avanzadas
    TimeoutSeconds = 300,        // Timeout global de peticiones
    UseAuth = true,              // Indica si debe adjuntar token Bearer
    UserAgent = "MyApp/1.0",     // User-Agent personalizado
    IsDeveloperMode = false      // Ignora errores de SSL en localhost
};
```

## Uso Básico

### Inicialización

```csharp
// Inyectar un logger (opcional)
var logger = new FileLog(new NewtonJson());
var http = new HttpService(config, logger);
```

### Prueba de Conectividad
Permite verificar rápidamente si el servidor es alcanzable:

```csharp
var health = await http.CheckAsync();
if (health.IsSuccess && health.Value) {
    // El servidor responde correctamente
}
```

### Operaciones CRUD Genéricas

```csharp
// GET
var result = await http.GetAsync<MiModelo>("api/datos/1");

// POST
var result = await http.PostAsync<MiReq, MiRes>("api/datos", payload);

// PUT / PATCH
await http.PutAsync<MiReq, object>("api/datos/1", updatePayload);
await http.PatchAsync<MiReq, object>("api/datos/1", partialPayload);

// DELETE
var deleted = await http.DeleteAsync("api/datos/1");
```

## Subida de Archivos

### Subida Nativa (Multipart)
Recomendado para archivos grandes. Utiliza `MultipartFormDataContent` internamente.

```csharp
var result = await http.UploadFileAsync("C:\\temp\\archivo.pdf", "api/upload");
```

### Subida Base64 (Estructura Personalizada)
Útil cuando la API espera un JSON con el contenido embebido:

```csharp
var uploadPayload = new {
    fileName = "documento.pdf",
    base64Content = Convert.ToBase64String(File.ReadAllBytes("ruta/al/archivo.pdf"))
};

var result = await http.PostAsync<object, object>("api/upload/base64", uploadPayload);
```

## Detalles Técnicos

### Manejo de Expiración del Token
El servicio es capaz de interpretar el campo `ExpirationKey` en varios formatos:
1. **Segundos Relativos**: (ej: `3600`) Expira en N segundos desde el momento actual.
2. **Unix Timestamp (s/ms)**: (ej: `1714856400`) Timestamp universal.
3. **ISO Date**: (ej: `"2024-05-04T20:00:00Z"`) Fecha absoluta.

### Modo Desarrollador
Cuando `IsDeveloperMode` es `true`, la librería omite la validación de certificados SSL, facilitando el desarrollo con APIs locales o certificados autofirmados.

---
*Versión: 1.1.7*
