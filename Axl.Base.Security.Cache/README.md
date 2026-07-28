# Axl.Base.Security.Cache

Librería de caché en memoria temporizada y segura para objetos `SecretValue`. Proporciona un ciclo de vida corto y controlado para secretos sensibles en memoria RAM, evitando que permanezcan de forma indefinida en texto plano.

## Seguridad en Memoria (Mitigación de Memory Dumps)
> [!IMPORTANT]
> A diferencia de las cachés en memoria clásicas que guardan los valores expuestos en texto plano, `SecretCache` encripta internamente los bytes de los secretos en la memoria RAM utilizando **DPAPI** (`ProtectedData.Protect` bajo `DataProtectionScope.LocalMachine`). 
> 
> De esta forma, si se realiza un volcado de memoria (Memory Dump) del proceso, los secretos de la caché no son visibles. Solo se desencriptan en un búfer temporal efímero en el microsegundo exacto de la petición, el cual es inmediatamente rellenado con ceros (`Array.Clear`) tras envolverse en el `SecretValue`.

## Prerequisites
- **Framework:** .NET Framework 4.0 o superior.
- **Dependencies:** `Axl.Base.Common` (para usar `SecretValue`).

---

## Technical Reference (API)

### Clase `SecretCache`
Clase estática hilo-segura (`lock`) para almacenar y recuperar de forma controlada objetos `SecretValue`.

#### `SecretValue GetOrAdd(string key, TimeSpan ttl, Func<SecretValue> retrieveFunc)`
Recupera un secreto desde la memoria caché estática. Si el secreto no existe o ha expirado, invoca el delegado físico suministrado, inicializa y guarda una copia de la caché, y devuelve el valor.
* **Parameters:**
  - `key`: Identificador único del secreto en la caché (búsqueda case-insensitive).
  - `ttl`: Tiempo de vida en caché (*Time-To-Live*) antes de ser marcado como expirado y destruido.
  - `retrieveFunc`: Función delegada ejecutada solo si la caché requiere actualización física.
* **Return:** Un objeto `SecretValue` independiente. El llamador es responsable de hacerle `Dispose()` cuando finalice su uso.

#### `void Invalidate(string key)`
Invalida y remueve inmediatamente un secreto específico de la caché en memoria, de forma que se destruye y sobrescribe con ceros su contenido.

#### `void Clear()`
Invalida, remueve y limpia de forma segura con ceros **todos** los secretos en memoria almacenados en la caché.

---

## Ejemplos de Uso Práctico

### Ejemplo 1: Integración en Clases de Configuración (Caso `Cfg.cs` con Credenciales de Broker)
En lugar de almacenar las contraseñas o usuarios sensibles descifrados de forma estática en propiedades `string` permanentes en memoria, se utiliza la caché para solicitarlas bajo demanda:

```csharp
using System;
using System.Linq;
using Axl.Base.Models;
using Axl.Base.Security.Cache.Services;

public class AppConfig
{
    private readonly string _dbPath = @"C:\Data\settings.db";
    
    // Parámetros públicos no-sensibles
    public string BrokerIp { get; set; }
    public int BrokerPort { get; set; }

    public AppConfig()
    {
        // Cargamos variables comunes en el constructor
        using (var sqlite = new SQLiteService(_dbPath))
        {
            var configList = sqlite.ReadVariables();
            
            BrokerIp = configList.FirstOrDefault(x => x.Key == "BROKERIP")?.Value ?? "127.0.0.1";
            
            if (int.TryParse(configList.FirstOrDefault(x => x.Key == "BROKERPORT")?.Value, out int port))
                BrokerPort = port;

            foreach (var cfg in configList) cfg.Dispose();
        }
    }

    /// <summary>
    /// Obtiene el usuario del Broker de forma segura desde la caché temporal
    /// </summary>
    public SecretValue GetBrokerUser()
    {
        return SecretCache.GetOrAdd("BROKERUSER", TimeSpan.FromMinutes(10), () =>
        {
            using (var sqlite = new SQLiteService(_dbPath))
            {
                // Simulación de lectura del valor cifrado en base de datos
                string decryptedUser = sqlite.ReadRawSecret("BROKERUSER");
                return new SecretValue(decryptedUser);
            }
        });
    }

    /// <summary>
    /// Obtiene la contraseña del Broker de forma segura desde la caché temporal
    /// </summary>
    public SecretValue GetBrokerPswd()
    {
        return SecretCache.GetOrAdd("BROKERPSWD", TimeSpan.FromMinutes(10), () =>
        {
            using (var sqlite = new SQLiteService(_dbPath))
            {
                string decryptedPswd = sqlite.ReadRawSecret("BROKERPSWD");
                return new SecretValue(decryptedPswd);
            }
        });
    }
}
```

#### Consumo de las Credenciales para Conectar el Cliente:
```csharp
public void ConnectToMqtt(AppConfig config)
{
    // Obtenemos los valores de la caché (solo existirán desencriptados dentro de este bloque)
    using (SecretValue user = config.GetBrokerUser())
    using (SecretValue password = config.GetBrokerPswd())
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(config.BrokerIp, config.BrokerPort)
            .WithCredentials(user.GetString(), password.GetString()) // Se decodifica y usa al vuelo
            .Build();

        _mqttClient.ConnectAsync(options);
    } 
    // Los datos sensibles en memoria RAM de 'user' y 'password' son sobrescritos con ceros aquí
}
```

---

### Ejemplo 2: Uso Directo con Inyección de Dependencias
```csharp
using System;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.RegistryStore.Services;
using Axl.Base.Security.Cache.Services;

public class DatabaseWatcher
{
    private readonly ISecretStore _store = new RegistryKeyStore(@"SOFTWARE\MyCompany\App");

    public void ProcessTask()
    {
        // Solicita el secreto a la caché estática con TTL de 10 minutos.
        // Solo leerá físicamente el Registro si no existe o ya pasaron los 10 minutos.
        using (SecretValue secret = SecretCache.GetOrAdd(
            "DbConnectionString", 
            TimeSpan.FromMinutes(10), 
            () => _store.RetrieveSecretSecure("DbConnectionString")))
        {
            string connectionString = secret.GetString();
            // Ejecutar consultas de base de datos...
        } // 'secret' se destruye y limpia sus bytes en memoria al salir del bloque
    }
}
```
