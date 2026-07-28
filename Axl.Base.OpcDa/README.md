# Tools.OpcDa

Librería para cliente OPC DA (Data Access) clásica.

## Prerequisites
- **Framework:** .NET Framework 4.0.
- **Arquitectura:** Requiere **x86** (32 bits). El proceso que consuma esta librería debe ejecutarse en modo 32 bits debido a las dependencias COM (OpcRcw/DCOM). Si se ejecuta en x64, se obtendrán errores de "Intento de leer o escribir en la memoria protegida".

## Technical Reference (API)

### Clase `OpcDaService`

#### Usage Example

```csharp
var opc = new OpcDaService();
var tags = new Dictionary<string, string> {
    { "T1", "Dev1.Static.Temp" },
    { "P1", "Dev1.Static.Press" }
};

// Lectura (Requiere proceso x86)
var res = opc.Read("PLC01", "Schneider-Aut.OFS.2", "127.0.0.1", tags, out string err);

if (string.IsNullOrEmpty(err)) {
    foreach (var item in res.Resultado)
        Console.WriteLine($"{item.Nombre}: {item.ValObj} ({item.Calidad})");
}
```

#### `Read(string deviceId, string svrName, string ip, Dictionary<string, string> parameters, out string error, int retry = 1)`
Realiza una lectura síncrona de múltiples tags.
- **Parámetros:**
  - `deviceId`: ID del equipo para bloqueo de tráfico exclusivo.
  - `svrName`: Nombre del servidor OPC (ej. `Schneider-Aut.OFS.2`).
  - `ip`: Dirección IP del servidor.
  - `parameters`: Diccionario donde la Clave es un nombre amigable y el Valor es el ItemID del OPC (ej. `{"Temp", "Dev1.Static.Var1"}`).
  - `retry`: Cantidad de reintentos si la calidad de los datos no es "Good".
- **Retorno:** Objeto `OPCRes`.
  - Contiene una lista `Resultado` de objetos `OPCObj` con `Nombre`, `ValObj` (el valor), `Fecha` y `Calidad`.
- **Salida:** `error` contiene detalles técnicos en caso de fallo crítico o si algunos tags no se pudieron leer.
