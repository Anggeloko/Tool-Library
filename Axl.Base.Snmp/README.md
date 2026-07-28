# Tools.Snmp

Librería de manipulación SNMP pre-empaquetada.

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Dependencias de NuGet
- `SnmpSharpNet` (v0.9.7)

## Technical Reference (API)

### Clase `SnmpService`

#### Ejemplo de Inicialización y Uso

```csharp
var snmp = new SnmpService();

// GET
var oids = new List<string> { "1.3.6.1.2.1.1.1.0" };
var results = snmp.Get("Switch01", "192.168.1.10", 161, 2, oids, out string err, "public");

if (string.IsNullOrEmpty(err)) {
    foreach (var kvp in results) {
        Console.WriteLine($"OID: {kvp.Key}, Value: {kvp.Value}");
    }
}

// WALK
var walkResults = snmp.Walk("Switch01", "192.168.1.10", 161, 2, "1.3.6.1.2.1.2.2.1.2", out string walkErr, "public");
```

#### `Get(string deviceId, string ip, int port, int version, List<string> oids, out string error, ...)`
Realiza una petición SNMP GET para obtener valores específicos de OIDs.
- **Parámetros:**
  - `deviceId`: ID del equipo (usado para bloqueo de tráfico).
  - `ip`: Dirección IP del agente SNMP.
  - `port`: Puerto (típicamente 161).
  - `version`: `1`, `2` o `3`.
  - `oids`: Lista de OIDs a consultar.
  - `error`: Parámetro de salida con el mensaje en caso de fallo.
- **Retorno:** `Dictionary<string, string>` (Clave: OID, Valor: Resultado).

#### `Walk(string deviceId, string ip, int port, int version, string oid, out string error, ...)`
Realiza una petición SNMP WALK (recorrido de árbol MIB).
- **Parámetros Igual que Get**, excepto que `oid` es el nodo raíz del recorrido.
- **Retorno:** `Dictionary<string, string>`.

#### `InterpretIfTypes(Dictionary<string, string> data, out Dictionary<int, ifTypeEl> dict)`
Mapea los resultados de una lectura de tipos de interfaz (`ifType`) a nombres legibles.
- **Retorno:** `Dictionary<int, int>` (Puerto -> TipoID).
