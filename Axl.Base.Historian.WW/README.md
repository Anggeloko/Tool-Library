# Axl.Base.Historian.WW

Standardized library for integrating with **Wonderware Historian** (AVEVA Historian) via optimized SQL queries.

## Features
- Real-time data acquisition (Live) and historical data retrieval (Cyclic).
- Extended quality rules support.
- Automatic **chunking** logic to handle large tag volumes without overloading SQL Server.
- Historian health check status.

## Installation
Reference the `Axl.Base.Historian.WW` project and ensure `Axl.Base.Common` and an `ISql` implementation (such as `Axl.Base.Database.MSSQL`) are included.

## Main Models

### `ValTag`
Represents an individual tag value.
- `Tag`: Tag name.
- `Actualizacion`: Timestamp.
- `Valor`: Numeric value (double?).
- `Unit`: Engineering units.
- `Quality`: Quality code.

### `SupTag`
Represents a supervised tag with metadata and its latest result.

## Basic Usage

### Service Configuration
```csharp
ISql db = new msSQL("HistorianServer", "192.168.1.10", 0, "Runtime", "sa", "password");
IWWHistorian historian = new WWHistorianService(db, chunkSize: 100);
```

### Live Data Acquisition
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

### Historical Data Acquisition (Cycles)
```csharp
DateTime end = DateTime.Now;
DateTime start = end.AddHours(-1);
var result = await historian.GetHistoricalValuesAsync(tags, start, end, cycleCount: 60);
```

### Health Check
```csharp
var badTags = await historian.GetHealthStatusAsync(onlyErrors: true);
```

## Testing Environment & Mockup (Endpoint Testing)

To facilitate development and library validation without requiring a physical connection to a Wonderware Historian server, a simulation (Mockup) SQL script is included at:
`Axl.Base.Historian.WW/Scripts/mockup_wonderware_historian.sql`

### Mockup Features
1. **Local Database**: Creates the `[Runtime]` database and required tables/views (`EngineeringUnit`, `AnalogTag`, `v_AnalogLive`, `History`, `v_Live`).
2. **Industrial Test Vector (109 Signals)**: Includes over 100 realistic signals distributed across 10 process stations (Boiler, Turbine, Generator, Cooling Tower, Condenser, BFP, Compressor, Substation, WTP, CEMS).
3. **Quality Simulation (Health Check)**: Introduces tags with bad quality codes (0 = Bad, 24 = Comm Failure, 28 = Out of Service, 64 = Uncertain) to validate `GetHealthStatusAsync` filtering.
4. **Historical Series**: Populated with 6 timestamped data points for each signal (from 60 minutes ago up to current time) to test cyclic mode in `GetHistoricalValuesAsync`.

### Steps for Local Testing
1. Run the `mockup_wonderware_historian.sql` script on a local SQL Server or LocalDB instance.
2. Configure the `ISql` connection string pointing to `localhost` and the `Runtime` database.
3. Call `IWWHistorian` interface methods to verify results returned by the simulated data source.

## Technical Notes
- Historical retrieval mode used is `Cyclic` with `wwQualityRule = 'Extended'`.
- The library assumes the SQL Server has the Wonderware `Runtime` database installed.
