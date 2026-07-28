# Axl.Base.Historian.WW

Librería estandarizada para la integración con **Wonderware Historian** (AVEVA Historian) a través de consultas SQL optimizadas.

## Características
- Adquisición de datos en tiempo real (Live) y datos históricos (Cyclic).
- Soporte para reglas de calidad extendidas.
- Lógica de **chunking** automática para manejar grandes volúmenes de etiquetas sin saturar SQL Server.
- Chequeo de salud del historiador (Health Status).

## Instalación
Referenciar el proyecto `Axl.Base.Historian.WW` y asegurarse de tener `Axl.Base.Common` y una implementación de `ISql` (como `Axl.Base.Database.MSSQL`).

## Modelos Principales

### `ValTag`
Representa un valor individual de una etiqueta.
- `Tag`: Nombre de la etiqueta.
- `Actualizacion`: Marca de tiempo.
- `Valor`: Valor numérico (double?).
- `Unit`: Unidades de ingeniería.
- `Quality`: Código de calidad.

### `SupTag`
Representa una etiqueta supervisada con metadatos y su último resultado.

## Uso Básico

### Configuración del Servicio
```csharp
ISql db = new msSQL("HistorianServer", "192.168.1.10", 0, "Runtime", "sa", "password");
IWWHistorian historian = new WWHistorianService(db, chunkSize: 100);
```

### Adquisición de Datos Vivos
```csharp
var tags = new List<string> { "Pump01.Speed", "Pump02.Speed" };
var result = await historian.GetLiveValuesAsync(tags);

if (result.IsSuccess)
{
    foreach (var val in result.Value)
    {
        Console.WriteLine($"{val.Tag}: {val.Valor} {val.Unit}");
    }
}
```

### Adquisición de Datos Históricos (Ciclos)
```csharp
DateTime end = DateTime.Now;
DateTime start = end.AddHours(-1);
var result = await historian.GetHistoricalValuesAsync(tags, start, end, cycleCount: 60);
```

### Chequeo de Salud
```csharp
var badTags = await historian.GetHealthStatusAsync(onlyErrors: true);
```

## Entorno de Pruebas y Mockup (Prueba de Endpoints)

Para facilitar el desarrollo y la validación de la librería sin requerir conexión a un servidor físico de Wonderware Historian, se incluye un script SQL de simulación (Mockup) ubicado en:
`Axl.Base.Historian.WW/Scripts/mockup_wonderware_historian.sql`

### Características del Mockup
1. **Base de Datos Local**: Crea la base de datos `[Runtime]` y las tablas/vistas requeridas (`EngineeringUnit`, `AnalogTag`, `v_AnalogLive`, `History`, `v_Live`).
2. **Vector de Pruebas Industrial (109 Señales)**: Incluye más de 100 señales realistas distribuidas en 10 estaciones de proceso (Boiler, Turbine, Generator, Cooling Tower, Condenser, BFP, Compressor, Substation, WTP, CEMS).
3. **Simulación de Calidad (Health Check)**: Introduce etiquetas con códigos de calidad defectuosos (0 = Bad, 24 = Comm Failure, 28 = Out of Service, 64 = Uncertain) para validar el filtrado del endpoint `GetHealthStatusAsync`.
4. **Series Históricas**: Poblado con 6 puntos en el tiempo para cada señal (desde hace 60 minutos hasta el tiempo actual) para probar el modo cíclico en `GetHistoricalValuesAsync`.

### Pasos para Probar Localmente
1. Ejecutar el script `mockup_wonderware_historian.sql` en un servidor SQL Server o LocalDB local.
2. Configurar la cadena de conexión en la instancia de `ISql` apuntando a `localhost` y la base de datos `Runtime`.
3. Ejecutar los métodos de la interfaz `IWWHistorian` para verificar los resultados devueltos por la fuente de datos simulada.

## Notas Técnicas
- El modo de recuperación histórico utilizado es `Cyclic` con `wwQualityRule = 'Extended'`.
- La librería asume que el servidor SQL tiene instalada la base de datos `Runtime` de Wonderware.
