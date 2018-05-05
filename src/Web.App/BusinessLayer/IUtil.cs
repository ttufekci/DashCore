using System;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.Linq;
using System.Threading.Tasks;
using Web.App.Models;

namespace Web.App.BusinessLayer
{
    public interface IUtil
    {
        Task<List<string>> GetTableList(CustomConnection customConnection);
        Task<List<string>> GetTableList(string connectionName);
        Task<List<TableColumnInfo>> GetColumnInfo(string connectionName, string tableName);
        Task<PagedData> GetTableDataList(List<TableColumnInfo> columnInfo, string connectionName, string tableName, int page = 0);
        Task<PagedData> GetTableDataListSearch(string connectionName, string tableName, int page, string searchColumn, string searchValue);
        Task<PagedData> GetTableDataListSearch(string connectionName, string tableName, int page, int pageSize, List<SearchFieldInfo> searchFields, string sortColumn, string sortDir, string sortDataType);
        Task<List<string>> GetSequenceList(CustomConnection customConnection);
        Task<List<string>> GetSequenceList(string connectionName);
        Task<Row> GetRowData(string connectionName, string tableName, string primaryKey, string tableColumnInfosJson);
        Task<Dictionary<string, List<string>>> GetTableGroups(List<string> tableList);
        Task<string> FindForeignDescription(OracleConnection oconn, string connectionName, string foreignTable, string foreignTableKeyColumn, string keyValue);
        Task<TableMetadata> GetTableMetadata(string tableName);
        Task ResetTableMetadata(string connectionName, string tableName);
        Task AddTableMetadata(string connectionName, string tableName);
    }
}
