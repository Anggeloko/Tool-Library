using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;
using Newtonsoft.Json;
using Npgsql;

namespace Axl.Base.Database.Postgres
{
    public class Postgres : ISql, ICheckable
    {
        private readonly string _connectionString;
        private readonly string _name;
        private readonly int _commandTimeout;

        public string Instance => _name;

        public Postgres(string connectionString, string name = "PostgresInstance", int commandTimeout = 30)
        {
            _name = name;
            _commandTimeout = commandTimeout;
            _connectionString = Encryption.Encrypt(connectionString, MachineInfo.Hash(32));
        }

        private async Task<NpgsqlConnection> CreateOpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(Encryption.Decrypt(_connectionString, MachineInfo.Hash(32)));
            await connection.OpenAsync();
            return connection;
        }

        public async Task<Result<string>> Header(string table, Dictionary<string, string> dict = null)
        {
            try
            {
                var sql = $"SELECT * FROM {table} LIMIT 0";
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new NpgsqlCommand(sql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var schema = reader.GetSchemaTable();
                    var columns = new List<string>();
                    foreach (DataRow row in schema.Rows)
                    {
                        columns.Add(row["ColumnName"].ToString());
                    }
                    return Result<string>.Success(string.Join("|", columns));
                }
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ExceptionUtils.Format(ex, $"Header-{table}"));
            }
        }

        public async Task<Result<int>> Execute(string sql, object parameters = null)
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    var affected = await command.ExecuteNonQueryAsync();
                    return Result<int>.Success(affected);
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, "Execute"));
            }
        }

        public async Task<Result<int>> Execute(string sql, Dictionary<string, object> parameters)
        {
            return await Execute(sql, (object)parameters);
        }

        public async Task<Result<T>> GetScalar<T>(string sql, object parameters = null)
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    var result = await command.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value) return Result<T>.Success(default);
                    return Result<T>.Success((T)Convert.ChangeType(result, typeof(T)));
                }
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ExceptionUtils.Format(ex, "GetScalar"));
            }
        }

        public async Task<Result<List<T>>> GetList<T>(string sql, object parameters = null) where T : new()
        {
            try
            {
                var list = new List<T>();
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapRowToObject<T>(reader));
                        }
                    }
                }
                return Result<List<T>>.Success(list);
            }
            catch (Exception ex)
            {
                return Result<List<T>>.Failure(ExceptionUtils.Format(ex, "GetList"));
            }
        }

        public async Task<Result<T>> GetFirstOrDefault<T>(string sql, object parameters = null) where T : class, new()
        {
            var result = await GetList<T>(sql, parameters);
            if (!result.IsSuccess) return Result<T>.Failure(result.Error);
            return Result<T>.Success(result.Value.FirstOrDefault());
        }

        public async Task<Result<List<Dictionary<string, object>>>> GetDict(string sql, object parameters = null)
        {
            try
            {
                var list = new List<Dictionary<string, object>>();
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (var index = 0; index < reader.FieldCount; index++)
                            {
                                row.Add(reader.GetName(index), reader.GetValue(index));
                            }
                            list.Add(row);
                        }
                    }
                }
                return Result<List<Dictionary<string, object>>>.Success(list);
            }
            catch (Exception ex)
            {
                return Result<List<Dictionary<string, object>>>.Failure(ExceptionUtils.Format(ex, "GetDict"));
            }
        }

        public async Task<Result<int>> BulkInsert(string tableName, DataTable data)
        {
            if (data == null || data.Rows.Count == 0) return Result<int>.Success(0);

            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    var columns = data.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\"").ToList();
                    var copySql = $"COPY \"{tableName}\" ({string.Join(", ", columns)}) FROM STDIN (FORMAT BINARY)";

                    using (var writer = connection.BeginBinaryImport(copySql))
                    {
                        foreach (DataRow row in data.Rows)
                        {
                            writer.StartRow();
                            foreach (DataColumn col in data.Columns)
                            {
                                writer.Write(row[col] ?? DBNull.Value);
                            }
                        }
                        writer.Complete();
                        return Result<int>.Success(data.Rows.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, $"BulkInsert-{tableName}"));
            }
        }

        public async Task<Result<int>> BulkInsert<T>(string tableName, IEnumerable<T> data) where T : new()
        {
            if (data == null || !data.Any()) return Result<int>.Success(0);
            return await BulkInsert(tableName, data.ToDataTable());
        }

        public async Task<Result<int>> PrepareAndBulkInsert(string tableName, DataTable data, bool truncateIfExists = true)
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    var cleanTableName = tableName.Replace("\"", "");

                    // 1. Obtener el esquema actual de la tabla en base de datos si existe
                    var actualColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var checkTableSql = @"
                        SELECT column_name, data_type 
                        FROM information_schema.columns 
                        WHERE table_name = @tableName AND table_schema = current_schema()";

                    using (var checkCmd = new NpgsqlCommand(checkTableSql, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@tableName", cleanTableName);
                        using (var reader = await checkCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                actualColumns[reader.GetString(0)] = reader.GetString(1);
                            }
                        }
                    }

                    bool schemasMatch = actualColumns.Count > 0 && actualColumns.Count == data.Columns.Count;
                    if (schemasMatch)
                    {
                        foreach (DataColumn column in data.Columns)
                        {
                            if (!actualColumns.ContainsKey(column.ColumnName))
                            {
                                schemasMatch = false;
                                break;
                            }
                        }
                    }

                    if (schemasMatch)
                    {
                        if (truncateIfExists)
                        {
                            var truncateSql = $"TRUNCATE TABLE \"{cleanTableName}\"";
                            using (var truncateCmd = new NpgsqlCommand(truncateSql, connection))
                            {
                                truncateCmd.CommandTimeout = _commandTimeout;
                                await truncateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    else
                    {
                        // 2. Si no coinciden o no existe, recrear la tabla
                        if (actualColumns.Count > 0)
                        {
                            var dropSql = $"DROP TABLE IF EXISTS \"{cleanTableName}\"";
                            using (var dropCmd = new NpgsqlCommand(dropSql, connection))
                            {
                                dropCmd.CommandTimeout = _commandTimeout;
                                await dropCmd.ExecuteNonQueryAsync();
                            }
                        }

                        var columnDefs = new List<string>();
                        foreach (DataColumn column in data.Columns)
                        {
                            var sqlDataType = GetSqlType(column.DataType);
                            var nullable = column.AllowDBNull ? "NULL" : "NOT NULL";
                            columnDefs.Add($"\"{column.ColumnName}\" {sqlDataType} {nullable}");
                        }

                        var createSql = $"CREATE TABLE \"{cleanTableName}\" ({string.Join(", ", columnDefs)})";
                        using (var createCmd = new NpgsqlCommand(createSql, connection))
                        {
                            createCmd.CommandTimeout = _commandTimeout;
                            await createCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 3. Ejecutar BulkInsert
                return await BulkInsert(tableName, data);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, $"PrepareAndBulkInsert-{tableName}"));
            }
        }

        private string GetSqlType(Type dataType)
        {
            if (dataType == typeof(string)) return "TEXT";
            if (dataType == typeof(int)) return "INTEGER";
            if (dataType == typeof(long)) return "BIGINT";
            if (dataType == typeof(short)) return "SMALLINT";
            if (dataType == typeof(byte)) return "SMALLINT"; // Postgres no tiene TINYINT puro de 1 byte
            if (dataType == typeof(decimal)) return "NUMERIC(18,4)";
            if (dataType == typeof(double)) return "DOUBLE PRECISION";
            if (dataType == typeof(float)) return "REAL";
            if (dataType == typeof(bool)) return "BOOLEAN";
            if (dataType == typeof(DateTime)) return "TIMESTAMP";
            if (dataType == typeof(Guid)) return "UUID";
            if (dataType == typeof(byte[])) return "BYTEA";

            return "TEXT";
        }


        public async Task<Result<int>> Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns) where T : new()
        {
            if (data == null || !data.Any()) return Result<int>.Success(0);
            if (keyColumns == null || keyColumns.Length == 0) return Result<int>.Failure("Key columns must be specified for Upsert.");

            var dt = data.ToDataTable();
            
            try
            {
                // Postgres soporta "INSERT ... ON CONFLICT", pero para ser consistente con el patrón de alto rendimiento
                // y manejar grandes volúmenes, usaremos una tabla temporal y un comando de UPSERT masivo.
                
                var stagingTable = $"staging_{Guid.NewGuid():N}";
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // 1. Crear tabla temporal
                    var createSql = $"CREATE TEMPORARY TABLE {stagingTable} (LIKE \"{tableName}\" INCLUDING ALL) ON COMMIT DROP";
                    using (var createCmd = new NpgsqlCommand(createSql, connection))
                    {
                        await createCmd.ExecuteNonQueryAsync();
                    }

                    // 2. Cargar datos vía Binary Copy
                    await BulkInsertInternal(stagingTable, dt, connection);

                    // 3. Ejecutar el UPSERT masivo
                    var columns = dt.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\"").ToList();
                    var updateParts = columns.Where(c => !keyColumns.Contains(c.Replace("\"", ""))).Select(c => $"{c} = EXCLUDED.{c}").ToList();
                    var matchCondition = string.Join(", ", keyColumns.Select(c => $"\"{c}\""));

                    var upsertSql = new StringBuilder();
                    upsertSql.Append($"INSERT INTO \"{tableName}\" ({string.Join(", ", columns)}) ");
                    upsertSql.Append($"SELECT {string.Join(", ", columns)} FROM {stagingTable} ");
                    upsertSql.Append($"ON CONFLICT ({matchCondition}) ");
                    
                    if (updateParts.Any())
                    {
                        upsertSql.Append($"DO UPDATE SET {string.Join(", ", updateParts)}");
                    }
                    else
                    {
                        upsertSql.Append("DO NOTHING");
                    }

                    using (var upsertCmd = new NpgsqlCommand(upsertSql.ToString(), connection))
                    {
                        upsertCmd.CommandTimeout = _commandTimeout * 3;
                        var affected = await upsertCmd.ExecuteNonQueryAsync();
                        return Result<int>.Success(affected);
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, $"Upsert-{tableName}"));
            }
        }

        public async Task<Result<int>> BulkMerge<T>(string destinationTable, IEnumerable<T> data, string[] keyColumns, string[] updateColumns = null, string[] ignoreColumns = null, int batchSize = 10000, int? timeoutSeconds = null) where T : new()
        {
            if (data == null || !data.Any()) return Result<int>.Success(0);
            return await BulkMerge(data.ToDataTable(), destinationTable, keyColumns, updateColumns, ignoreColumns, batchSize, timeoutSeconds);
        }

        public async Task<Result<int>> BulkMerge(DataTable dataTable, string destinationTable, string[] keyColumns, string[] updateColumns = null, string[] ignoreColumns = null, int batchSize = 10000, int? timeoutSeconds = null)
        {
            if (dataTable == null || dataTable.Rows.Count == 0) return Result<int>.Success(0);
            if (keyColumns == null || keyColumns.Length == 0)
                return Result<int>.Failure("Debe especificar al menos una columna clave para el cruce del MERGE.");

            int effectiveTimeout = timeoutSeconds ?? Math.Max(_commandTimeout * 3, 600);
            string stagingTable = $"tmp_merge_{Guid.NewGuid():N}";
            string cleanDestination = destinationTable.Replace("\"", "");

            try
            {
                // 1. Filtrar las columnas a procesar (excluyendo las especificadas en ignoreColumns)
                var validColumns = dataTable.Columns.Cast<DataColumn>()
                    .Select(c => c.ColumnName)
                    .Where(c => ignoreColumns == null || !ignoreColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (validColumns.Count == 0)
                    return Result<int>.Failure("No hay columnas válidas para realizar el BulkMerge.");

                using (var connection = await CreateOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 2. Crear tabla temporal con las columnas requeridas basada en la tabla destino
                            string colsList = string.Join(", ", validColumns.Select(c => $"\"{c}\""));
                            var createSql = $"CREATE TEMPORARY TABLE {stagingTable} AS SELECT {colsList} FROM \"{cleanDestination}\" WHERE 1=0;";
                            using (var createCmd = new NpgsqlCommand(createSql, connection, transaction))
                            {
                                createCmd.CommandTimeout = effectiveTimeout;
                                await createCmd.ExecuteNonQueryAsync();
                            }

                            // 3. Cargar datos vía Binary Copy
                            var copySql = $"COPY {stagingTable} ({colsList}) FROM STDIN (FORMAT BINARY)";
                            using (var writer = connection.BeginBinaryImport(copySql))
                            {
                                foreach (DataRow row in dataTable.Rows)
                                {
                                    writer.StartRow();
                                    foreach (var colName in validColumns)
                                    {
                                        writer.Write(row[colName] ?? DBNull.Value);
                                    }
                                }
                                writer.Complete();
                            }

                            // 4. Ejecutar el MERGE/UPSERT masivo
                            var colsToUpdate = (updateColumns ?? validColumns.Except(keyColumns, StringComparer.OrdinalIgnoreCase)).ToList();
                            var updateParts = colsToUpdate.Select(c => $"\"{c}\" = EXCLUDED.\"{c}\"").ToList();
                            var matchCondition = string.Join(", ", keyColumns.Select(c => $"\"{c}\""));

                            var upsertSql = new StringBuilder();
                            upsertSql.Append($"INSERT INTO \"{cleanDestination}\" ({colsList}) ");
                            upsertSql.Append($"SELECT {colsList} FROM {stagingTable} ");
                            upsertSql.Append($"ON CONFLICT ({matchCondition}) ");

                            if (updateParts.Any())
                            {
                                upsertSql.Append($"DO UPDATE SET {string.Join(", ", updateParts)};");
                            }
                            else
                            {
                                upsertSql.Append("DO NOTHING;");
                            }

                            int affectedRows = 0;
                            using (var upsertCmd = new NpgsqlCommand(upsertSql.ToString(), connection, transaction))
                            {
                                upsertCmd.CommandTimeout = effectiveTimeout;
                                affectedRows = await upsertCmd.ExecuteNonQueryAsync();
                            }

                            // 5. Eliminar tabla temporal y confirmar transacción
                            using (var dropCmd = new NpgsqlCommand($"DROP TABLE IF EXISTS {stagingTable};", connection, transaction))
                            {
                                dropCmd.CommandTimeout = effectiveTimeout;
                                await dropCmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            return Result<int>.Success(affectedRows);
                        }
                        catch (Exception ex)
                        {
                            try { transaction.Rollback(); } catch { }
                            return Result<int>.Failure(ExceptionUtils.Format(ex, $"BulkMerge-Transaction-{destinationTable}"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, $"BulkMerge-{destinationTable}"));
            }
        }

        private async Task BulkInsertInternal(string tableName, DataTable data, NpgsqlConnection connection)
        {
            var columns = data.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\"").ToList();
            var copySql = $"COPY {tableName} ({string.Join(", ", columns)}) FROM STDIN (FORMAT BINARY)";

            using (var writer = connection.BeginBinaryImport(copySql))
            {
                foreach (DataRow row in data.Rows)
                {
                    writer.StartRow();
                    foreach (DataColumn col in data.Columns)
                    {
                        writer.Write(row[col] ?? DBNull.Value);
                    }
                }
                writer.Complete();
            }
        }

        private void AddParameters(NpgsqlCommand command, object parameters)
        {
            if (parameters == null) return;

            if (parameters is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    command.Parameters.AddWithValue(kvp.Key.StartsWith("@") ? kvp.Key : "@" + kvp.Key, kvp.Value ?? DBNull.Value);
                }
            }
            else
            {
                var properties = parameters.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    command.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
                }
            }
        }

        private T MapRowToObject<T>(IDataRecord record) where T : new()
        {
            var obj = new T();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columnNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            for (var i = 0; i < record.FieldCount; i++) columnNames.Add(record.GetName(i));

            foreach (var prop in properties)
            {
                if (columnNames.Contains(prop.Name) && prop.CanWrite)
                {
                    var value = record[prop.Name];
                    if (value != DBNull.Value)
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        prop.SetValue(obj, Convert.ChangeType(value, targetType), null);
                    }
                }
            }
            return obj;
        }

        public async Task<Result<bool>> CheckAsync()
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    return Result<bool>.Success(true);
                }
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ExceptionUtils.Format(ex, $"Check-{_name}"));
            }
        }
    }
}
