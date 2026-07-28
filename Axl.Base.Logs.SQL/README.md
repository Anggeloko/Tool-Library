# Tools.Logs.SQL

Logging extension for database persistence.

## Prerequisites
- **Framework:** .NET Framework 4.5.2.

## Technical Reference (API)

### Class `SQLog`

Implements `ILog` and requires an `ISql` instance to operate.

#### Usage Example

```csharp
ISql db = new MSSQL("LogDB", "server", 1433, "Logs", "user", "pass");
IJson json = new NewtonJson();

ILog sqlLog = new SQLog(json, db, "app_logs");

sqlLog.Write("Event logged into SQL");
```

#### Constructor: `SQLog(ISql sqlService, string table = "logs")`
- **Parameters:**
  - `sqlService`: Any `ISql` implementation (`MSSQL`, `MySQL`, `SQLite`).
  - `table`: Name of the database table where log entries will be inserted.

#### Functionality
Whenever `Write` or any of its variations is called, an asynchronous insertion into the database is executed containing the fields:
- `Timestamp`: Date and time.
- `Level`: `INFO`, `ERROR`, `WARN`, `DEBUG`.
- `Caller`: Name of the issuing method.
- `Message`: Log entry content.
- `Exception`: Details of exception if present.
