using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Web.App.BusinessLayer;
using Web.App.Models;

namespace Web.App.Controllers
{
    public class EditController : Controller
    {
        private readonly CustomConnectionContext _context;
        private readonly IUtil _util;

        public EditController(CustomConnectionContext context, IUtil util)
        {
            _context = context;
            _util = util;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> EditData(string connectionName, string tableName, string id, int page, int tableRowIndx, string searchFields = "",string sortColumn = "", string sortDir = "")
        {
            var tableDataVM = new TableDataVM
            {
                TableList = await _util.GetTableList(connectionName),
                TableName = tableName,
                ConnectionName = connectionName
            };

            tableDataVM.TableGroups = await _util.GetTableGroups(tableDataVM.TableList);

            var columnList = new List<TableColumnInfo>();
            var tableDataDict = new PagedData { Data = new Dictionary<int, Row>() };

            columnList = await _util.GetColumnInfo(connectionName, tableName);

            tableDataDict = await _util.GetTableDataList(columnList, connectionName, tableName, page);

            var row = tableDataDict.Data[tableRowIndx];
            var tableColumnInfosJson = row.TableColumnInfosJson;

            var tablemetadata = await _util.GetTableMetadata(connectionName, tableName);
            tableDataVM.SequenceName = tablemetadata.SequenceName;

            tableDataVM.ColumnList = columnList;
            tableDataVM.TableDataList = tableDataDict;
            tableDataVM.RowData = await _util.GetRowData(connectionName, tableName, id, tableColumnInfosJson);
            tableDataVM.TableColumnInfosJson = tableColumnInfosJson;
            tableDataVM.SortColumn = sortColumn;
            tableDataVM.SortDir = sortDir;

            var searchFieldsArray = JsonConvert.DeserializeObject<List<SearchFieldInfo>>(searchFields);

            foreach (var field in searchFieldsArray)
            {
                tableDataVM.SearchValues[field.Name] = field.Value;
            }

            tableDataVM.SearchFields = searchFields;

            return View(tableDataVM);
        }

        private static string GetPrimaryKey(List<TableColumnInfo> columnList, string[] values)
        {
            var primaryKey = "";
            for(int i=0; i < columnList.Count(); i++)
            {
                if (columnList[i].IsPrimaryKey)
                {
                    primaryKey += values[i] + ";";
                }                
            }

            primaryKey = primaryKey.TrimEnd(';');
            return primaryKey;
        }

        [HttpPost]
        public async Task<IActionResult> EditDataPost(string connectionName, string tableName, IEnumerable<string> dataFields, IEnumerable<string> oldDataFields, string tableColumnInfosJson)
        {
            var tableDataVM = new TableDataVM
            {
                TableList = await _util.GetTableList(connectionName),
                TableName = tableName,
                ConnectionName = connectionName
            };

            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);
            var connectionString = Util.GetConnectionString(customConnection);

            tableDataVM.TableGroups = await _util.GetTableGroups(tableDataVM.TableList);

            var columnList = new List<TableColumnInfo>();
            var tableDataDict = new PagedData { Data = new Dictionary<int, Row>() };
            columnList = await _util.GetColumnInfo(connectionName, tableName);

            var tablemetadata = await _util.GetTableMetadata(connectionName, tableName);

            tableDataVM.ColumnList = columnList;
            tableDataVM.TableDataList = tableDataDict;
            tableDataVM.SequenceName = tablemetadata.SequenceName;

            var dataFieldArray = dataFields.ToArray();

            var oldDataFieldArray = oldDataFields.ToArray();

            var primaryKey = GetPrimaryKey(columnList, dataFieldArray);

            var whereColumnListStmt = "";

            if (string.IsNullOrEmpty(primaryKey))
            {
                var oldColumnList = JsonConvert.DeserializeObject<List<TableColumnInfo>>(tableColumnInfosJson).ToArray();
                var builderWhere = new System.Text.StringBuilder();
                builderWhere.Append(whereColumnListStmt);

                for (int j = 0; j < columnList.Count(); j++)
                {
                    builderWhere.Append(columnList[j].Name + "='" + oldColumnList[j].Value + "' and ");
                }
                whereColumnListStmt = builderWhere.ToString();

                whereColumnListStmt = whereColumnListStmt.TrimEnd(' ', 'd', 'n', 'a');
            }

            var columnListStmt = "";
            var builder = new System.Text.StringBuilder();
            builder.Append(columnListStmt);

            for (int j = 0; j < columnList.Count(); j++)
            {
                if (columnList[j].IsPrimaryKey) continue;                

                if (columnList[j].DataType.Equals("DATE"))
                {
                    builder.Append(columnList[j].Name + "=TO_DATE('" + dataFieldArray[j] + "','dd.mm.yyyy HH24:MI:SS'), "); ;
                }
                else
                {
                    builder.Append(columnList[j].Name + "='" + dataFieldArray[j] + "', "); ;
                }
            }

            columnListStmt = builder.ToString();

            columnListStmt = columnListStmt.TrimEnd(' ').TrimEnd(',');

            var updateSqlStmt = "";

            var whereStmt = Util.FindUniqueRowWhereStmt(primaryKey, columnList);

            updateSqlStmt = string.IsNullOrEmpty(primaryKey) ? "update " + tableName + " set " + columnListStmt + " where " + whereColumnListStmt : "update " + tableName + " set " + columnListStmt + " where " + whereStmt;

            var sessionHistorySql = new SessionSqlHistory
            {
                EventDate = DateTime.Now,
                SqlText = updateSqlStmt,
                BasicSqlText = updateSqlStmt
            };

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                using (var cmd = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = updateSqlStmt,
                    CommandType = CommandType.Text
                })
                {
                    var result = cmd.ExecuteNonQuery();
                }
            }

            await _context.SessionSqlHistory.AddAsync(sessionHistorySql);
            await _context.SaveChangesAsync();

            var tableColumnInfos = new List<TableColumnInfo>();

            for (int j = 0; j < columnList.Count(); j++)
            {
                var tableColumnInfo = new TableColumnInfo
                {
                    DataType = columnList[j].DataType,
                    IsPrimaryKey = columnList[j].IsPrimaryKey,
                    Name = columnList[j].Name,
                    Value = dataFieldArray[j],
                    OldValue = columnList[j].OldValue
                };

                tableColumnInfos.Add(tableColumnInfo);
            }

            var newTableColumnInfosJson = JsonConvert.SerializeObject(tableColumnInfos);

            tableDataVM.RowData = await _util.GetRowData(connectionName, tableName, primaryKey, newTableColumnInfosJson);
            tableDataVM.TableColumnInfosJson = newTableColumnInfosJson;

            ViewBag.Message = "Successfully saved";

            return View(nameof(EditData), tableDataVM);
        }

        [HttpGet]
        public async Task<JsonResult> FindForeignDescription(string connectionName, string foreignTable, string foreignTableKey, string foreignKeyValue)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var columnList = new List<TableColumnInfo>();

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                var desc = await _util.FindForeignDescription(oconn, connectionName, foreignTable, foreignTableKey, foreignKeyValue);
                return Json(new { success = true, message = desc });
            }            
        }

        [HttpPost]
        public async Task<JsonResult> DeleteRowAsync(string connectionName, string tableName, string id, int page, int tableRowIndx, string searchFields)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);
            var connectionString = Util.GetConnectionString(customConnection);

            var tableDataDict = new PagedData { Data = new Dictionary<int, Row>() };

            var searchFieldsArray = JsonConvert.DeserializeObject<List<SearchFieldInfo>>(searchFields);

            var columnList = await _util.GetColumnInfo(connectionName, tableName);

            tableDataDict = searchFieldsArray.Any() ? await _util.GetTableDataListSearch(connectionName, tableName, 0, searchFieldsArray[0].Name, searchFieldsArray[0].Value) : await _util.GetTableDataList(columnList, connectionName, tableName, page);

            var row = tableDataDict.Data[tableRowIndx];
            var tableColumnInfosJson = row.TableColumnInfosJson;

            var whereStmt = Util.FindUniqueRowWhereStmt(id, columnList);

            var deleteSqlStmt = "delete " + tableName + " where " + whereStmt;

            if (string.IsNullOrEmpty(id))
            {
                var whereColumnListStmt = "";

                var oldColumnList = JsonConvert.DeserializeObject<List<TableColumnInfo>>(tableColumnInfosJson).ToArray();
                var builder = new System.Text.StringBuilder();
                builder.Append(whereColumnListStmt);

                for (int j = 0; j < oldColumnList.Count(); j++)
                {
                    builder.Append(oldColumnList[j].Name + "='" + oldColumnList[j].Value + "' and ");
                }
                whereColumnListStmt = builder.ToString();

                whereColumnListStmt = whereColumnListStmt.TrimEnd(' ', 'd', 'n', 'a');

                deleteSqlStmt = "delete " + tableName + " where " + whereColumnListStmt;
            }

            var sessionHistorySql = new SessionSqlHistory
            {
                EventDate = DateTime.Now,
                SqlText = deleteSqlStmt,
                BasicSqlText = deleteSqlStmt
            };

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                using (var cmd = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = deleteSqlStmt,
                    CommandType = CommandType.Text
                })
                {
                    var result = cmd.ExecuteNonQuery();
                }
            }

            await _context.SessionSqlHistory.AddAsync(sessionHistorySql);
            await _context.SaveChangesAsync();

            return Json(true);
        }

        public async Task<IActionResult> LookupSearch(string parentColumn, string connectionName, string tableName, string tableColumn, int page = 0)
        {
            var tableDataVM = new TableDataVM
            {
                TableList = await _util.GetTableList(connectionName),
                TableName = tableName,
                ConnectionName = connectionName
            };

            tableDataVM.TableGroups = await _util.GetTableGroups(tableDataVM.TableList);

            var columnList = new List<TableColumnInfo>();
            var tableDataDict = new PagedData { Data = new Dictionary<int, Row>() };

            columnList = await _util.GetColumnInfo(connectionName, tableName);

            tableDataDict = await _util.GetTableDataList(columnList, connectionName, tableName, page);

            tableDataVM.ColumnList = columnList;
            tableDataVM.TableDataList = tableDataDict;
            tableDataVM.ForeignTableColumn = tableColumn;
            tableDataVM.ParentColumn = parentColumn;
            tableDataVM.PagerStart = Util.FindPagerStart(page);

            return View(tableDataVM);
        }

        public async Task<IActionResult> TableDataSearchLookup(string parentColumn, string connectionName, string tableName, string tableColumn, string searchFields, int page=0, int pageSize = 10, string sortColumn = "", string sortDir = "", string sortColumnDataType = "")
        {
            var tableDataVM = new TableDataVM
            {
                TableList = await _util.GetTableList(connectionName),
                TableName = tableName,
                ConnectionName = connectionName
            };

            var columnList = new List<TableColumnInfo>();
            var tableDataDict = new PagedData { Data = new Dictionary<int, Row>() };

            tableDataVM.TableGroups = await _util.GetTableGroups(tableDataVM.TableList);

            var searchFieldsArray = JsonConvert.DeserializeObject<List<SearchFieldInfo>>(searchFields);

            if (tableName != "undefined")
            {
                columnList = await _util.GetColumnInfo(connectionName, tableName);
                tableDataDict = await _util.GetTableDataListSearch(connectionName, tableName, page, pageSize, searchFieldsArray, sortColumn, sortDir, sortColumnDataType);
            };

            tableDataVM.ColumnList = columnList;
            tableDataVM.TableDataList = tableDataDict;

            foreach (var field in searchFieldsArray)
            {
                tableDataVM.SearchValues[field.Name] = field.Value;
            }

            tableDataVM.PagerStart = Util.FindPagerStart(page);
            tableDataVM.SortColumn = sortColumn;
            tableDataVM.SortDir = sortDir;
            tableDataVM.ForeignTableColumn = tableColumn;
            tableDataVM.ParentColumn = parentColumn;

            return View("LookupSearch", tableDataVM);
        }

        public async Task<IActionResult> TableDataLookup(string connectionName, string tableName = "undefined", int page = 0)
        {
            var tableDataVM = new TableDataVM
            {
                TableList = await _util.GetTableList(connectionName),
                TableName = tableName,
                ConnectionName = connectionName
            };

            tableDataVM.TableGroups = await _util.GetTableGroups(tableDataVM.TableList);

            var columnList = new List<TableColumnInfo>();
            var tableDataDict = new PagedData { Data = new Dictionary<int, Row>() };

            if (tableName != "undefined")
            {
                columnList = await _util.GetColumnInfo(connectionName, tableName);
                tableDataDict = await _util.GetTableDataList(columnList, connectionName, tableName, page);
            };

            tableDataVM.ColumnList = columnList;
            tableDataVM.TableDataList = tableDataDict;

            return View("LookupSearch", tableDataVM);
        }

        public JsonResult ClearCache(string connectionName, string tableName)
        {
            Util.ConnectionColumnInfo = new Dictionary<string, List<TableColumnInfo>>();
            Util.ConnectionTableList = new Dictionary<string, List<string>>();
            return Json(true);
        }

        public async Task<IActionResult> ShowSqlHistory(string connectionName)
        {
            var sessionSqlHistory = await _context.SessionSqlHistory.OrderByDescending(x => x.EventDate).ToListAsync();
            return View(sessionSqlHistory);
        }

        public async Task<IActionResult> EditBlobField(string connectionName, string table, string columnName, string primaryKey, string primaryKeyValue)
        {
            var editBlobFieldVM = new EditBlobFieldVM
            {
                ConnectionName = connectionName,
                Table = table,
                ColumnName = columnName,
                PrimaryKeyColumn = primaryKey,
                PrimaryKeyValue = primaryKeyValue
            };
            
            return View(editBlobFieldVM);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(string connectionName, IFormFile file, string table, string columnName, string primaryKey)
        {
            // full path to file in temp location
            var filePath = Path.GetTempFileName();

            if (file.Length > 0)
            {
                //using (var stream = new FileStream(filePath, FileMode.Create))
                //{
                //    await file.CopyToAsync(stream);
                //}

                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var AvatarImage = memoryStream.ToArray();
                }
            }            

            // process uploaded files
            // Don't rely on or trust the FileName property without validation.

            return Ok(new { count = filePath });
        }
    }
}