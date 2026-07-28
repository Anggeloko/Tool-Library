using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Statics;

namespace Axl.Base.Database.msSQL
{
    public class MsSQL : ISql, ICheckable
    {
        private readonly string _connectionString;
        private readonly string _name;
        private readonly int _commandTimeout;
        private readonly Dictionary<string, HashSet<string>> _columnCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// Inicializa una nueva instancia del servicio de datos de SQL Server mediante un string de conexión.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión completa.</param>
        /// <param name="name">Identificador de la instancia.</param>
        /// <param name="timeout">Tiempo de espera para los comandos en segundos.</param>
        public MsSQL(string connectionString, string name = "MsSQL", int timeout = 30)
        {
            _name = name;
            _commandTimeout = timeout;
            _connectionString = Encryption.Encrypt(connectionString, MachineInfo.Hash(32));
        }

        public string Instance => _name;

        #region --- Helpers de Diccionario ---

        public string DictString(Dictionary<string, object> dict, string field) => dict.TryGetValue(field, out object data) && data != null && data != DBNull.Value ? data.ToString() : string.Empty;
        public int DictInt(Dictionary<string, object> dict, string field) => dict.TryGetValue(field, out object data) && data != null && data != DBNull.Value ? Convert.ToInt32(data) : -1;
        public double DictDouble(Dictionary<string, object> dict, string field) => dict.TryGetValue(field, out object data) && data != null && data != DBNull.Value ? Convert.ToDouble(data) : -1;
        public DateTime DictDateTime(Dictionary<string, object> dict, string field) => dict.TryGetValue(field, out object data) && data != null && data != DBNull.Value ? Convert.ToDateTime(data) : DateTime.MinValue;

        #endregion

        private async Task<SqlConnection> CreateOpenConnectionAsync()
        {
            var connection = new SqlConnection(Encryption.Decrypt(_connectionString, MachineInfo.Hash(32)));
            await connection.OpenAsync();
            return connection;
        }

        private void AddParameters(SqlCommand command, object parameters)
        {
            if (parameters == null) return;
            foreach (var prop in parameters.GetType().GetProperties())
            {
                var paramName = "@" + prop.Name;
                var value = prop.GetValue(parameters, null) ?? DBNull.Value;
                command.Parameters.AddWithValue(paramName, value);
            }
        }

        private void AddParameters(SqlCommand command, Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;
            foreach (var param in parameters)
            {
                var paramName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
                var value = param.Value ?? DBNull.Value;
                command.Parameters.AddWithValue(paramName, value);
            }
        }

        #region --- Implementación ISql ---

        public async Task<Result<string>> Header(string table, Dictionary<string, string> dict = null)
        {
            try
            {
                if (!_columnCache.TryGetValue(table, out var actualColumns))
                {
                    actualColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var sqlQuery = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName";
                    using (var connection = await CreateOpenConnectionAsync())
                    using (var command = new SqlCommand(sqlQuery, connection))
                    {
                        command.CommandTimeout = _commandTimeout;
                        command.Parameters.AddWithValue("@tableName", table);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync()) actualColumns.Add(reader.GetString(0));
                        }
                    }
                    _columnCache[table] = actualColumns;
                }

                if (dict == null || dict.Count == 0) return Result<string>.Success("*");

                var selectParts = new List<string>();
                foreach (var column in dict)
                {
                    if (actualColumns.Contains(column.Key))
                    {
                        if (!string.IsNullOrEmpty(column.Value) && column.Key != column.Value)
                            selectParts.Add($"[{column.Key}] AS [{column.Value}]");
                        else
                            selectParts.Add($"[{column.Key}]");
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
                using (var command = new SqlCommand(sql, connection))
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
                using (var command = new SqlCommand(sql, connection))
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
                using (var command = new SqlCommand(sql, connection))
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
                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var isPrimitive = typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal) || typeof(T) == typeof(DateTime);
                        while (await reader.ReadAsync())
                        {
                            if (isPrimitive)
                            {
                                var val = reader.GetValue(0);
                                results.Add(val == DBNull.Value ? default(T) : (T)Convert.ChangeType(val, typeof(T)));
                            }
                            else
                            {
                                results.Add(MapRowToObject<T>(reader));
                            }
                        }
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
                using (var command = new SqlCommand(sql, connection))
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
                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = _commandTimeout;
                    AddParameters(command, parameters);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
                            for (var index = 0; index < reader.FieldCount; index++)
                                row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
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
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    using (var bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = tableName.StartsWith("[") ? tableName : $"[{tableName}]";
                        bulkCopy.BatchSize = 5000;
                        bulkCopy.BulkCopyTimeout = _commandTimeout * 2;

                        foreach (DataColumn column in data.Columns)
                            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                        await bulkCopy.WriteToServerAsync(data);
                        return Result<int>.Success(data.Rows.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, $"BulkInsert-{tableName}"));
            }
        }

        public async Task<Result<int>> PrepareAndBulkInsert(string tableName, DataTable data, bool truncateIfExists = true)
        {
            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    var cleanTableName = tableName.Replace("[", "").Replace("]", "");
                    
                    // 1. Obtener el esquema actual de la tabla en base de datos si existe
                    var actualColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var checkTableSql = @"
                        SELECT COLUMN_NAME, DATA_TYPE 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = @tableName";
                        
                    using (var checkCmd = new SqlCommand(checkTableSql, connection))
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
                        // Verificar que todas las columnas del DataTable existan en la DB
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
                        // Si coinciden y se solicita limpiar
                        if (truncateIfExists)
                        {
                            var truncateSql = $"TRUNCATE TABLE [{cleanTableName}]";
                            using (var truncateCmd = new SqlCommand(truncateSql, connection))
                            {
                                truncateCmd.CommandTimeout = _commandTimeout;
                                await truncateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    else
                    {
                        // 2. Si no coinciden o no existe, recrear la tabla
                        // Borrar si existe
                        if (actualColumns.Count > 0)
                        {
                            var dropSql = $"DROP TABLE [{cleanTableName}]";
                            using (var dropCmd = new SqlCommand(dropSql, connection))
                            {
                                dropCmd.CommandTimeout = _commandTimeout;
                                await dropCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Crear nueva tabla mapeando tipos
                        var columnDefs = new List<string>();
                        foreach (DataColumn column in data.Columns)
                        {
                            var sqlDataType = GetSqlType(column.DataType);
                            var nullable = column.AllowDBNull ? "NULL" : "NOT NULL";
                            columnDefs.Add($"[{column.ColumnName}] {sqlDataType} {nullable}");
                        }

                        var createSql = $"CREATE TABLE [{cleanTableName}] ({string.Join(", ", columnDefs)})";
                        using (var createCmd = new SqlCommand(createSql, connection))
                        {
                            createCmd.CommandTimeout = _commandTimeout;
                            await createCmd.ExecuteNonQueryAsync();
                        }

                        // Invalidar caché de columnas de la clase si existe
                        lock (_columnCache)
                        {
                            if (_columnCache.ContainsKey(cleanTableName))
                            {
                                _columnCache.Remove(cleanTableName);
                            }
                        }
                    }
                }

                // 3. Ejecutar la inserción masiva (BulkInsert)
                return await BulkInsert(tableName, data);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, $"PrepareAndBulkInsert-{tableName}"));
            }
        }

        private string GetSqlType(Type dataType)
        {
            if (dataType == typeof(string)) return "NVARCHAR(MAX)";
            if (dataType == typeof(int)) return "INT";
            if (dataType == typeof(long)) return "BIGINT";
            if (dataType == typeof(short)) return "SMALLINT";
            if (dataType == typeof(byte)) return "TINYINT";
            if (dataType == typeof(decimal)) return "DECIMAL(18,4)";
            if (dataType == typeof(double)) return "FLOAT";
            if (dataType == typeof(float)) return "REAL";
            if (dataType == typeof(bool)) return "BIT";
            if (dataType == typeof(DateTime)) return "DATETIME2";
            if (dataType == typeof(Guid)) return "UNIQUEIDENTIFIER";
            if (dataType == typeof(byte[])) return "VARBINARY(MAX)";
            
            return "NVARCHAR(MAX)";
        }


        public async Task<Result<int>> BulkInsert<T>(string tableName, IEnumerable<T> data) where T : new()
        {
            if (data == null || !data.Any()) return Result<int>.Success(0);
            return await BulkInsert(tableName, data.ToDataTable());
        }

        public async Task<Result<int>> Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns) where T : new()
        {
            if (data == null || !data.Any()) return Result<int>.Success(0);
            if (keyColumns == null || keyColumns.Length == 0) return Result<int>.Failure("Key columns must be specified for Upsert.");

            var dt = data.ToDataTable();
            var stagingTable = $"#Staging_{Guid.NewGuid():N}";

            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // 1. Crear tabla temporal igual a la original (solo esquema)
                    var createTableSql = $"SELECT TOP 0 * INTO [{stagingTable}] FROM [{tableName}]";
                    using (var createCmd = new SqlCommand(createTableSql, connection))
                    {
                        await createCmd.ExecuteNonQueryAsync();
                    }

                    // 2. Bulk Insert a la tabla temporal
                    using (var bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = stagingTable;
                        bulkCopy.BatchSize = 5000;
                        bulkCopy.BulkCopyTimeout = _commandTimeout * 2;
                        foreach (DataColumn column in dt.Columns)
                            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                        await bulkCopy.WriteToServerAsync(dt);
                    }

                    // 3. MERGE
                    var matchCondition = string.Join(" AND ", keyColumns.Select(c => $"T.[{c}] = S.[{c}]"));
                    var columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                    var updateParts = columns.Where(c => !keyColumns.Contains(c)).Select(c => $"T.[{c}] = S.[{c}]").ToList();

                    // Si no hay columnas para actualizar (solo llaves), el MATCHED no hace nada o se omite
                    var updateClause = updateParts.Any() 
                        ? $"WHEN MATCHED THEN UPDATE SET {string.Join(", ", updateParts)}" 
                        : "";

                    var mergeSql = $@"
                        MERGE INTO [{tableName}] AS T
                        USING [{stagingTable}] AS S
                        ON ({matchCondition})
                        {updateClause}
                        WHEN NOT MATCHED THEN
                            INSERT ({string.Join(", ", columns.Select(c => $"[{c}]"))})
                            VALUES ({string.Join(", ", columns.Select(c => $"S.[{c}]"))});";

                    using (var mergeCmd = new SqlCommand(mergeSql, connection))
                    {
                        mergeCmd.CommandTimeout = _commandTimeout * 3;
                        var affected = await mergeCmd.ExecuteNonQueryAsync();
                        return Result<int>.Success(affected);
                    }
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(ExceptionUtils.Format(ex, $"Upsert-{tableName}"));
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

