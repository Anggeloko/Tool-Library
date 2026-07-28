# Tools.Wmi

Librería de manipulación WMI pre-empaquetada para extracción de métricas de Windows.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Technical Reference (API)

### Clase `WmiService`

#### Usage Example

```csharp
var wmi = new WmiService();
var query = new WmiQuery {
    Nmspace = "root\\cimv2",
    CLspace = "Win32_OperatingSystem",
    Queries = new Dictionary<string, string> {
        { "OS", "Caption" },
        { "Version", "Version" }
    }
};

// Lectura local
var results = wmi.Read("LocalHost", "127.0.0.1", "", "", query, out string err);

if (string.IsNullOrEmpty(err)) {
    foreach (var row in results)
        Console.WriteLine($"OS: {row["OS"]}, Ver: {row["Version"]}");
}
```

#### `Read(string deviceId, string ip, string user, string password, WmiQuery wma, out string error, int port = 0)`
Realiza consultas WQL a un host local o remoto.
- **Parámetros:**
  - `deviceId`: ID del equipo (para bloqueo de tráfico).
  - `ip`: IP del host remoto o `localhost`.
  - `user` / `password`: Credenciales. Si es local, se omiten automáticamente.
  - `wma`: Objeto `WmiQuery` que define el namespace, la clase y las propiedades a consultar.
  - `error`: Mensaje en caso de fallo.
  - `port`: (Opcional) Puerto de conexión personalizado. Si es `0` o no es un puerto TCP válido (1-65535), se utiliza el comportamiento por defecto de WMI/DCOM.
- **Retorno:** `List<Dictionary<string, string>>`. Cada diccionario representa una instancia encontrada, con los mapeos definidos en `wma`.

### Model `WmiQuery`
- **Propiedades:**
  - `NMspace`: Namespace de WMI (ej. `root\cimv2`).
  - `CLspace`: Clase WMI (ej. `Win32_Processor`).
  - `Queries`: `Dictionary<string, string>` donde la Clave es el nombre amigable y el Valor es la propiedad real de WMI.
