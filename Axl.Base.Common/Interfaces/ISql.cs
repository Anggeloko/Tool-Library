using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Axl.Base.Models;

namespace Axl.Base.Interfaces
{
    /// <summary>
    /// Define el contrato para un servicio de acceso a datos, permitiendo
    /// ejecutar consultas y mapear resultados de forma genérica.
    /// </summary>
    public interface ISql
    {
        string Instance { get; }
        Task<Result<string>> Header(string table, Dictionary<string, string> dict = null);
        Task<Result<int>> Execute(string sql, object parameters = null);
        Task<Result<int>> Execute(string sql, Dictionary<string, object> parameters);
        Task<Result<T>> GetScalar<T>(string sql, object parameters = null);
        Task<Result<List<T>>> GetList<T>(string sql, object parameters = null) where T : new();
        Task<Result<T>> GetFirstOrDefault<T>(string sql, object parameters = null) where T : class, new();
        Task<Result<List<Dictionary<string, object>>>> GetDict(string sql, object parameters = null);
        Task<Result<int>> BulkInsert(string tableName, DataTable data);
        Task<Result<int>> BulkInsert<T>(string tableName, IEnumerable<T> data) where T : new();
        Task<Result<int>> PrepareAndBulkInsert(string tableName, DataTable data, bool truncateIfExists = true);
        Task<Result<int>> Upsert<T>(string tableName, IEnumerable<T> data, string[] keyColumns) where T : new();
    }
}


