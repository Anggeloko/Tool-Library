# Axl.Base.RegistryStore

Librería de almacenamiento seguro de secretos y contraseñas cifradas en el Registro de Windows usando DPAPI a nivel de máquina.

## Prerequisites
- **Framework:** .NET Framework 4.0 o superior.
- **Dependencies:** `System.Security` para el cifrado por DPAPI (`ProtectedData`).

## Technical Reference (API)

### Interfaz `ISecretStore`
Contrato estándar expuesto por `Axl.Base.Interfaces` para la inyección de dependencias y desacoplamiento del almacenamiento.

```csharp
public interface ISecretStore
{
    void StoreSecret(string keyName, string secretText);
    void StoreSecret(string keyName, SecretValue secret);
    string RetrieveSecret(string keyName);
    SecretValue RetrieveSecretSecure(string keyName);
    void DeleteSecret(string keyName);
}
```

---

### Clase `SecretValue`
Clase envoltorio para datos sensibles que se asegura de sobrescribir los bytes internos con ceros cuando se destruye o se libera.

- **Métodos:**
  - `SecretValue(string secretText)`: Crea la instancia a partir de un string.
  - `SecretValue(byte[] utf8Bytes)`: Crea la instancia a partir de un array de bytes.
  - `GetString()`: Decodifica y retorna el secreto en formato string.
  - `GetBytes()`: Retorna una copia del array de bytes subyacente.
  - `Dispose()`: Limpia y llena con ceros el array de bytes interno.

---

### Clase `RegistryKeyStore`
Implementa `ISecretStore` y maneja la escritura física en claves del Registro bajo `HKLM` (Registry 64-bit), además de gestionar automáticamente los permisos (ACLs) de lectura y escritura de Windows.

#### Usage Example (Instalador / Proceso Elevado)
Cifra y guarda una clave de API, otorgando permisos de solo lectura para la cuenta de servicio local `.\MikeUser`.
```csharp
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.RegistryStore.Services;

// Paso de inicialización (Requiere ejecutarse como Administrador)
ISecretStore store = new RegistryKeyStore(@"SOFTWARE\SchneiderElectric\ServiceManager", null, @".\MikeUser");

// Almacenar secreto de forma segura usando SecretValue
using (var secret = new SecretValue("SuperSecurePassword123!"))
{
    store.StoreSecret("DatabaseConnectionString", secret);
}
```

#### Usage Example (Servicio de Fondo / Proceso de lectura)
El servicio que se ejecuta bajo la cuenta `.\MikeUser` puede leer la clave sin necesidad de privilegios elevados de Administrador ni de conocer el secreto del cifrado:
```csharp
ISecretStore store = new RegistryKeyStore(@"SOFTWARE\SchneiderElectric\ServiceManager");

using (SecretValue secret = store.RetrieveSecretSecure("DatabaseConnectionString"))
{
    string connString = secret.GetString();
    // Uso de la conexión...
} // El destructor/Dispose limpia automáticamente el buffer de memoria
```

#### `RegistryKeyStore(string registryPath, byte[] entropy = null, string serviceAccountName = null)`
- **Parámetros:**
  - `registryPath`: La ruta de la clave en HKLM (ej. `SOFTWARE\MiCompania\App`).
  - `entropy`: Array de bytes opcional que agrega una capa adicional de secreto al cifrado DPAPI local. Debe coincidir al escribir y leer.
  - `serviceAccountName`: Nombre de la cuenta (ej. `.\ServiceUser` o `MYDOMAIN\User`) que obtendrá permisos explícitamente de lectura en la clave del Registro.

#### `void DeleteStoreTree()`
Elimina recursivamente todo el árbol de subclaves de este almacén bajo HKLM.
> [!IMPORTANT]
> Este método contiene medidas de seguridad (guardrails) y arrojará un `SecurityException` si la ruta del registro es demasiado corta o no empieza por `SOFTWARE` (requiere al menos 3 niveles de profundidad, por ejemplo, `SOFTWARE\Company\App`), para evitar borrados accidentales de ramas clave del sistema.
