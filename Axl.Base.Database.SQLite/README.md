# Tools.Database.SQLite

Implementación del cliente para SQLite v3 orientada a persistencia local de configuraciones y variables de forma segura (encriptada).

## Prerequisites
- **Framework:** .NET Framework 4.0.

## Dependencias de NuGet
- `System.Data.SQLite.Core` (v1.0.119)
- `Newtonsoft.Json` (v13.0.4)

## Technical Reference (API)

### Clase `SQLiteService`

Gestiona el almacenamiento de variables en una base de datos SQLite local (`settings.db`), utilizando encriptación AES-256 basada en el hardware de la máquina.

#### Usage Example

```csharp
// Se inicializa con la ruta donde se guardará el settings.db
var settings = new SQLiteService("C:\\ProgramData\\MyApp");

// Guardar (Se encripta automáticamente)
settings.SetVariable("ApiKey", "12345-ABCDE");

// Recuperar (Se desencripta automáticamente)
string key = settings.GetVariable("ApiKey", "default_if_not_found");

// Leer todo
var all = settings.ReadVariables();
```

#### `GetVariable(string key, string defaultValue = null)`
Recupera el valor de una variable específica.
- **Auto-Curación:** Si el dato fue guardado con una identidad de máquina anterior (ej. cambio de nombre), el método lo re-encripta automáticamente con la identidad actual.
- **Retorno:** `string` (valor desencriptado o `defaultValue`).

#### `SetVariable(string key, string value)`
Guarda o actualiza una variable específica. Asegura que no existan duplicados eliminando registros previos asociados a la misma llave bajo cualquier hash de máquina conocido.

#### `DeleteVariable(string key)`
Elimina una variable de la base de datos.

#### `ReadVariables()`
Lee todas las variables almacenadas en la base de datos.
- **Retorno:** `List<VariableItem>`.

#### `SaveVariables(IEnumerable<VariableItem> variables)`
Guarda una colección completa de variables, sobrescribiendo el contenido actual de la tabla.

---

### Model `VariableItem`

Clase base para el manejo de pares clave-valor con soporte para notificación de cambios.

- **`Key`**: Identificador de la variable.
- **`Value`**: Valor de la variable (encriptado automáticamente al persistir).
