using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Database.MySQL
{
    public class MySQL : ISql, ICheckable
    {
        private readonly string _connectionString;
        private readonly string _name;
        private readonly int _commandTimeout;
        private readonly Dictionary<string, HashSet<string>> _columnCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);


        public MySQL(string connectionString, string name = "MySQL", int timeout = 30)
        {
            _name = name;
            _commandTimeout = timeout;
            _connectionString = Encryption.Encrypt(connectionString, MachineInfo.Hash(32));
        }

        public string Instance => _name;

        private async Task<MySqlConnection> CreateOpenConnectionAsync()
        {
            var connection = new MySqlConnection(Encryption.Decrypt(_connectionString, MachineInfo.Hash(32)));
            await connection.OpenAsync();
            return connection;
        }

        private void AddParameters(MySqlCommand command, object parameters)
        {
            if (parameters == null) return;
            foreach (var prop in parameters.GetType().GetProperties())
            {
                var paramName = "@" + prop.Name;
                var value = prop.GetValue(parameters, null) ?? DBNull.Value;
                command.Parameters.AddWithValue(paramName, value);
            }
        }

        private void AddParameters(MySqlCommand command, Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;
            foreach (var param in parameters)
            {
                var paramName = param.Key.StartsWith("@") || param.Key.StartsWith("?") ? param.Key : $"@{param.Key}";
                var value = param.Value ?? DBNull.Value;
                command.Parameters.AddWithValue(paramName, value);
            }
        }

        #region --- Implementación ISql ---

        public async Task<Result<string>> Header(string table, Dictionary<string, string> dict = null)
        {
            try
            {
                if (!_columnCache.TryGetValue(table, out var actuals))
                {
                    actuals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    using (var connection = await CreateOpenConnectionAsync())
                    using (var command = new MySqlCommand($"SHOW COLUMNS FROM `{table}`;", connection))
                    {
                        command.CommandTimeout = _commandTimeout;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync()) actuals.Add(reader.GetString(0));
                        }
                    }
                    _columnCache[table] = actuals;
                }

                if (dict == null || dict.Count == 0) return Result<string>.Success("*");

                var selectParts = new List<string>();
                foreach (var column in dict)
                {
                    if (actuals.Contains(column.Key))
                    {
                        if (!string.IsNullOrEmpty(column.Value) && column.Key != column.Value)
                            selectParts.Add($"`{column.Key}` AS `{column.Value}`");
                        else
                            selectParts.Add($"`{column.Key}`");
                    }
                }
                return Result<string>.Success(selectParts.Count > 0 ? string.Join(", ", selectParts) : "*");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ExceptionUtils.Format(ex, $"Table: {table}"));
            }
        }

        public async Task<Result<int>> Execute(string sql, object parameters = null)
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    return Result<int>.Success(await command.ExecuteNonQueryAsync());
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, "Execute-Object"));
            }
        }

        public async Task<Result<int>> Execute(string sql, Dictionary<string, object> parameters)
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    return Result<int>.Success(await command.ExecuteNonQueryAsync());
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, "Execute-Dict"));
            }
        }

        public async Task<Result<T>> GetScalar<T>(string sql, object parameters = null)
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    object resultObj = await command.ExecuteScalarAsync();
                    if (resultObj == null || resultObj is DBNull) return Result<T>.Success(default);
                    return Result<T>.Success((T)Convert.ChangeType(resultObj, typeof(T)));
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
                var results = new List<T>();
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) results.Add(MapRowToObject<T>(reader));
                    }
                }
                return Result<List<T>>.Success(results);
            }
            catch (Exception ex)
            {
                return Result<List<T>>.Failure(ExceptionUtils.Format(ex, "GetList"));
            }
        }

        public async Task<Result<T>> GetFirstOrDefault<T>(string sql, object parameters = null) where T : class, new()
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync()) return Result<T>.Success(MapRowToObject<T>(reader));
                    }
                }
                return Result<T>.Success(null);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ExceptionUtils.Format(ex, "GetFirstOrDefault"));
            }
        }

        public async Task<Result<List<Dictionary<string, object>>>> GetDict(string sql, object parameters = null)
        {
            try
            {
                var results = new List<Dictionary<string, object>>();
                using (var connection = await CreateOpenConnectionAsync())
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
                            for (var i = 0; i < reader.FieldCount; i++)
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            results.Add(row);
                        }
                    }
                }
                return Result<List<Dictionary<string, object>>>.Success(results);
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
                    int totalRows = await BulkInsertInternal(tableName, data, connection);
                    return Result<int>.Success(totalRows);
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, $"BulkInsert-{tableName}"));
            }
        }

        private async Task<int> BulkInsertInternal(string tableName, DataTable data, MySqlConnection connection)
        {
            int totalRows = 0;
            int batchSize = 500;

            var columnNames = new List<string>();
            foreach (DataColumn col in data.Columns) columnNames.Add($"`{col.ColumnName}`");

            string columnsPart = string.Join(", ", columnNames);

            int index = 0;
            while (index < data.Rows.Count)
            {
                int currentBatchCount = Math.Min(batchSize, data.Rows.Count - index);
                var sqlBuilder = new StringBuilder();
                sqlBuilder.Append($"INSERT INTO `{tableName}` ({columnsPart}) VALUES ");

                using (var command = new MySqlCommand())
                {
                    command.Connection = connection;
                    command.CommandTimeout = _commandTimeout * 2;

                    var valueRows = new List<string>();
                    for (int rowIndex = 0; rowIndex < currentBatchCount; rowIndex++)
                    {
                        DataRow row = data.Rows[index + rowIndex];
                        var paramNames = new List<string>();
                        for (int colIndex = 0; colIndex < data.Columns.Count; colIndex++)
                        {
                            string paramName = $"@p{rowIndex}_{colIndex}";
                            command.Parameters.AddWithValue(paramName, row[colIndex] ?? DBNull.Value);
                            paramNames.Add(paramName);
                        }
                        valueRows.Add($"({string.Join(", ", paramNames)})");
                    }

                    sqlBuilder.Append(string.Join(", ", valueRows));
                    sqlBuilder.Append(";");

                    command.CommandText = sqlBuilder.ToString();
                    totalRows += await command.ExecuteNonQueryAsync();
                }
                index += currentBatchCount;
            }
            return totalRows;
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
                    var cleanTableName = tableName.Replace("`", "");

                    // 1. Obtener el esquema actual de la tabla en base de datos si existe
                    var actualColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var checkTableSql = @"
                        SELECT COLUMN_NAME, DATA_TYPE 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName";

                    using (var checkCmd = new MySqlCommand(checkTableSql, connection))
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
                            var truncateSql = $"TRUNCATE TABLE `{cleanTableName}`";
                            using (var truncateCmd = new MySqlCommand(truncateSql, connection))
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
                            var dropSql = $"DROP TABLE IF EXISTS `{cleanTableName}`";
                            using (var dropCmd = new MySqlCommand(dropSql, connection))
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
                            columnDefs.Add($"`{column.ColumnName}` {sqlDataType} {nullable}");
                        }

                        var createSql = $"CREATE TABLE `{cleanTableName}` ({string.Join(", ", columnDefs)})";
                        using (var createCmd = new MySqlCommand(createSql, connection))
                        {
                            createCmd.CommandTimeout = _commandTimeout;
                            await createCmd.ExecuteNonQueryAsync();
                        }

                        lock (_columnCache)
                        {
                            if (_columnCache.ContainsKey(cleanTableName))
                            {
                                _columnCache.Remove(cleanTableName);
                            }
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
            if (dataType == typeof(string)) return "LONGTEXT";
            if (dataType == typeof(int)) return "INT";
            if (dataType == typeof(long)) return "BIGINT";
            if (dataType == typeof(short)) return "SMALLINT";
            if (dataType == typeof(byte)) return "TINYINT";
            if (dataType == typeof(decimal)) return "DECIMAL(18,4)";
            if (dataType == typeof(double)) return "DOUBLE";
            if (dataType == typeof(float)) return "FLOAT";
            if (dataType == typeof(bool)) return "TINYINT(1)";
            if (dataType == typeof(DateTime)) return "DATETIME(6)";
            if (dataType == typeof(Guid)) return "VARCHAR(36)";
            if (dataType == typeof(byte[])) return "LONGBLOB";

            return "LONGTEXT";
        }


        public async Task<Result<int>> Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns) where T : new()
        {
            if (data == null || !data.Any()) return Result<int>.Success(0);
            if (keyColumns == null || keyColumns.Length == 0) return Result<int>.Failure("Key columns must be specified for Upsert.");

            var dt = data.ToDataTable();
            var stagingTable = $"Staging_{Guid.NewGuid():N}";

            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // 1. Crear tabla temporal igual a la original (solo esquema)
                    var createTableSql = $"CREATE TEMPORARY TABLE `{stagingTable}` SELECT * FROM `{tableName}` WHERE 1=0";
                    using (var createCmd = new MySqlCommand(createTableSql, connection))
                    {
                        await createCmd.ExecuteNonQueryAsync();
                    }

                    // 2. Cargar datos en la tabla staging
                    await BulkInsertInternal(stagingTable, dt, connection);

                    // 3. UPDATE JOIN
                    var columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                    var updateParts = columns.Where(c => !keyColumns.Contains(c)).Select(c => $"T.`{c}` = S.`{c}`").ToList();
                    var matchCondition = string.Join(" AND ", keyColumns.Select(c => $"T.`{c}` = S.`{c}`"));

                    int affected = 0;
                    if (updateParts.Any())
                    {
                        var updateSql = $@"
                            UPDATE `{tableName}` AS T
                            INNER JOIN `{stagingTable}` AS S ON {matchCondition}
                            SET {string.Join(", ", updateParts)}";
                        using (var updateCmd = new MySqlCommand(updateSql, connection))
                        {
                            affected += await updateCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // 4. INSERT (solo los que no existen)
                    var insertSql = $@"
                        INSERT INTO `{tableName}` ({string.Join(", ", columns.Select(c => $"`{c}`"))})
                        SELECT {string.Join(", ", columns.Select(c => $"S.`{c}`"))}
                        FROM `{stagingTable}` AS S
                        LEFT JOIN `{tableName}` AS T ON {matchCondition}
                        WHERE T.`{keyColumns[0]}` IS NULL";

                    using (var insertCmd = new MySqlCommand(insertSql, connection))
                    {
                        affected += await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Result<int>.Success(affected);
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
            string cleanDestination = destinationTable.Replace("`", "");

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
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // 2. Crear tabla temporal igual a la estructura de las columnas requeridas
                            string colsList = string.Join(", ", validColumns.Select(c => $"`{c}`"));
                            var createTableSql = $"CREATE TEMPORARY TABLE `{stagingTable}` SELECT {colsList} FROM `{cleanDestination}` WHERE 1=0";
                            using (var createCmd = new MySqlCommand(createTableSql, connection, transaction))
                            {
                                createCmd.CommandTimeout = effectiveTimeout;
                                await createCmd.ExecuteNonQueryAsync();
                            }

                            // 3. Cargar datos en la tabla temporal en lotes respetando batchSize y límite de parámetros
                            int batchCountLimit = Math.Min(batchSize, Math.Max(1, 60000 / validColumns.Count));
                            int index = 0;
                            while (index < dataTable.Rows.Count)
                            {
                                int currentBatchCount = Math.Min(batchCountLimit, dataTable.Rows.Count - index);
                                var sqlBuilder = new StringBuilder();
                                sqlBuilder.Append($"INSERT INTO `{stagingTable}` ({colsList}) VALUES ");

                                using (var insertCmd = new MySqlCommand())
                                {
                                    insertCmd.Connection = connection;
                                    insertCmd.Transaction = transaction;
                                    insertCmd.CommandTimeout = effectiveTimeout;

                                    var valueRows = new List<string>();
                                    for (int rowIndex = 0; rowIndex < currentBatchCount; rowIndex++)
                                    {
                                        DataRow row = dataTable.Rows[index + rowIndex];
                                        var paramNames = new List<string>();
                                        for (int colIndex = 0; colIndex < validColumns.Count; colIndex++)
                                        {
                                            string colName = validColumns[colIndex];
                                            string paramName = $"@p{rowIndex}_{colIndex}";
                                            insertCmd.Parameters.AddWithValue(paramName, row[colName] ?? DBNull.Value);
                                            paramNames.Add(paramName);
                                        }
                                        valueRows.Add($"({string.Join(", ", paramNames)})");
                                    }

                                    sqlBuilder.Append(string.Join(", ", valueRows));
                                    sqlBuilder.Append(";");

                                    insertCmd.CommandText = sqlBuilder.ToString();
                                    await insertCmd.ExecuteNonQueryAsync();
                                }
                                index += currentBatchCount;
                            }

                            // 4. UPDATE JOIN
                            var colsToUpdate = (updateColumns ?? validColumns.Except(keyColumns, StringComparer.OrdinalIgnoreCase)).ToList();
                            var updateParts = colsToUpdate.Select(c => $"T.`{c}` = S.`{c}`").ToList();
                            var matchCondition = string.Join(" AND ", keyColumns.Select(c => $"T.`{c}` = S.`{c}`"));

                            int affectedRows = 0;
                            if (updateParts.Any())
                            {
                                var updateSql = $@"
                                    UPDATE `{cleanDestination}` AS T
                                    INNER JOIN `{stagingTable}` AS S ON {matchCondition}
                                    SET {string.Join(", ", updateParts)};";
                                using (var updateCmd = new MySqlCommand(updateSql, connection, transaction))
                                {
                                    updateCmd.CommandTimeout = effectiveTimeout;
                                    affectedRows += await updateCmd.ExecuteNonQueryAsync();
                                }
                            }

                            // 5. INSERT (registros que no existen)
                            var insertSql = $@"
                                INSERT INTO `{cleanDestination}` ({colsList})
                                SELECT {string.Join(", ", validColumns.Select(c => $"S.`{c}`"))}
                                FROM `{stagingTable}` AS S
                                LEFT JOIN `{cleanDestination}` AS T ON {matchCondition}
                                WHERE T.`{keyColumns[0]}` IS NULL;";

                            using (var insertCmd = new MySqlCommand(insertSql, connection, transaction))
                            {
                                insertCmd.CommandTimeout = effectiveTimeout;
                                affectedRows += await insertCmd.ExecuteNonQueryAsync();
                            }

                            // 6. Eliminar tabla temporal y confirmar transacción
                            using (var dropCmd = new MySqlCommand($"DROP TEMPORARY TABLE IF EXISTS `{stagingTable}`;", connection, transaction))
                            {
                                dropCmd.CommandTimeout = effectiveTimeout;
                                await dropCmd.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();
                            return Result<int>.Success(affectedRows);
                        }
                        catch (Exception ex)
                        {
                            try { await transaction.RollbackAsync(); } catch { }
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

        #endregion

        #region --- Implementación ICheckable ---

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

        #endregion

        private T MapRowToObject<T>(IDataRecord record) where T : new()
        {
            var obj = new T();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columnNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 0; i < record.FieldCount; i++) columnNames.Add(record.GetName(i));

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
    }
}

