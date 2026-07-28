# Tools.NetworkFileAccess

Librería para el acceso seguro a archivos y carpetas en red (UNC) utilizando suplantación de identidad (impersonation) de Windows. Permite realizar operaciones de sistema de archivos en recursos compartidos que requieren credenciales específicas.

## Características

- **Suplantación de Identidad**: Ejecuta operaciones bajo el contexto de un usuario de red específico.
- **Smart Copy/Move**: Gestiona automáticamente transferencias híbridas entre rutas de red y locales, utilizando un puente de memoria para evitar conflictos de permisos.
- **Validación UNC**: Asegura que las rutas de red sigan el formato estándar `\\servidor\recurso`.
- **Verificación de Salud**: Soporta `ICheckable` para validar la accesibilidad de un share de red.
- **Compatibilidad**: Diseñada para .NET Framework 4.5.2+.

## Uso Básico

### Inicialización

Para usar el servicio, primero debe crearse un `UserSecurityContext`.

```csharp
using Tools.Security;
using Tools.NetworkFileAccess.Services;
using Tools.Statics;

// 1. Crear el contexto de seguridad
var securePass = WindowsHelper.ToSecureString("mi_password");
var context = new UserSecurityContext("usuario", securePass, LogonType.NewCredentials, "dominio");

// 2. Instanciar el servicio
var networkService = new NetworkFileAccessService(context);
```

### Operaciones de Archivo

```csharp
// Listar archivos
string[] files = networkService.ListFiles(@"\\servidor\share\datos");

// Escribir un archivo (crea directorios automáticamente si no existen)
byte[] data = System.Text.Encoding.UTF8.GetBytes("Hola Red");
networkService.WriteFile(@"\\servidor\share\logs\test.txt", data);

// Lectura
byte[] readData = networkService.ReadFile(@"\\servidor\share\logs\test.txt");
```

### Smart Copy (Red <-> Local)

La librería detecta si el origen o destino es local y maneja el cambio de contexto de seguridad automáticamente:

```csharp
// De Red a Local (Copia segura)
networkService.Copy(@"\\servidor\share\config.ini", @"C:\AppData\config.ini");

// De Local a Red
networkService.Copy(@"C:\Logs\local.log", @"\\servidor\share\respaldos\local.log");
```

## Verificación de Acceso

Puede verificar si una ruta es accesible antes de operar:

```csharp
var perms = networkService.TestAccess(@"\\servidor\share");
if (perms.HasFlag(DirectoryPermissions.Write)) {
    // Tenemos permisos de escritura
}

// O vía ICheckable
networkService.CheckPath = @"\\servidor\share";
var result = await networkService.CheckAsync();
```

## Lógica Interna

### Gestión de Contextos
Cuando se realiza una copia entre Red y Local, la librería:
1.  Entra en el contexto suplantado para leer/escribir en la red.
2.  Sale del contexto suplantado para leer/escribir en el disco local.
Esto previene el error común de "Acceso denegado" que ocurre cuando un usuario de red intenta escribir en el disco C: local.

---
*Versión: 1.0.0*
*Dependencia: Tools.Common 1.1.1*
