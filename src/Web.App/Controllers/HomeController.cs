using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Web.App.BusinessLayer;
using Web.App.Models;

namespace Web.App.Controllers
{
    public class HomeController : Controller
    {
        private readonly CustomConnectionContext _context;
        private readonly IUtil _util;

        public HomeController(CustomConnectionContext context, IUtil util)
        {
            _context = context;
            _util = util;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.CustomConnection.ToListAsync());
        }

        public IActionResult AddConnection()
        {
            var connectionVM = new CustomConnection();
            return View(connectionVM);
        }

        public async Task<IActionResult> EditConnection(long? Id)
        {
            if (Id == null)
            {
                return NotFound();
            }

            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(m => m.Id == Id);
            if (customConnection == null)
            {
                return NotFound();
            }
            return View("AddConnection", customConnection);
        }

        public async Task<IActionResult> RemoveConnection(long? Id)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(m => m.Id == Id);
            _context.CustomConnection.Remove(customConnection);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddConnection(CustomConnection connection)
        {
            var info = Request.Form["process"];

            Console.WriteLine(info);

            if (info == "test")
            {
                var connectionString = Util.GetConnectionString(connection);
                var oconn = new OracleConnection(connectionString);

                oconn.Open();
                oconn.Dispose();

                ViewBag.ConnectionSuccess = "Connection Success";

                return View(connection);
            }

            if (ModelState.IsValid)
            {
                var tableList = await _util.GetTableList(connection);
                var sequenceList = await _util.GetSequenceList(connection);

                foreach (var table in tableList)
                {
                    var tablemetadata = await _context.TableMetadata.SingleOrDefaultAsync(x => x.TableName == table);

                    if (tablemetadata == null)
                    {
                        tablemetadata = new TableMetadata();
                    }

                    tablemetadata.TableName = table;
                    tablemetadata.SequenceName = Util.FindBestMatch(table, sequenceList);

                    if (tablemetadata.Id > 0)
                    {
                        _context.Update(tablemetadata);
                    }
                    else
                    {
                        _context.Add(tablemetadata);
                    }
                }

                if (connection.Id > 0)
                {
                    _context.Update(connection);
                }
                else
                {
                    _context.Add(connection);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(connection);
        }

        private async Task<List<string>> GetSequenceList(string connectionName)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var sequenceList = new List<string>();

            const string sequenceListSql = "select SEQUENCE_NAME from user_sequences order by SEQUENCE_NAME";

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                using (var cmd = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = sequenceListSql,
                    CommandType = CommandType.Text
                })
                {
                    var dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        sequenceList.Add(dr.GetString(0));
                    }
                }
            }

            return sequenceList;
        }

        public async Task<IActionResult> TableData(string connectionName, string tableName="undefined", int page=0)
        {
            var tableDataVM = new TableDataVM
            {
                TableList = await _util.GetTableList(connectionName),
                TableName = tableName,
                ConnectionName = connectionName
            };

            var assemblyVersion = typeof(Startup).Assembly.GetName().Version.ToString();

            tableDataVM.Version = assemblyVersion;

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
            tableDataVM.PagerStart = Util.FindPagerStart(page);

            ViewBag.ConnectionInfo = connectionName;

            return View(tableDataVM);
        }

        public async Task<IActionResult> TableDataSearch(string connectionName, string tableName, string searchFields, int page=0, int pageSize = 10, string sortColumn="", string sortDir="", string sortColumnDataType="")
        {
            var tableDataVM = new TableDataVM
            {
                TableList = await _util.GetTableList(connectionName),
                TableName = tableName,
                ConnectionName = connectionName
            };

            var assemblyVersion = typeof(Startup).Assembly.GetName().Version.ToString();

            tableDataVM.Version = assemblyVersion;

            var columnList = new List<TableColumnInfo>();
            var tableDataDict = new PagedData { Data = new Dictionary<int, Row>() };

            var searchFieldsArray = JsonConvert.DeserializeObject<List<SearchFieldInfo>>(searchFields);

            tableDataVM.TableGroups = await _util.GetTableGroups(tableDataVM.TableList);

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

            tableDataVM.SortColumn = sortColumn;
            tableDataVM.SortDir = sortDir;
            tableDataVM.PagerStart = Util.FindPagerStart(page);

            return View("TableData", tableDataVM);
        }

        public async Task<IActionResult> AddData(string connectionName, string tableName, int page, string searchFields = "", string sortColumn = "", string sortDir = "")
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

            var tablemetadata = await _context.TableMetadata.SingleOrDefaultAsync(x => x.TableName == tableName);

            if (tablemetadata == null)
            {
                tablemetadata = new TableMetadata
                {
                    TableName = tableName
                };
                var sequenceList = await _util.GetSequenceList(connectionName);
                tablemetadata.SequenceName = Util.FindBestMatch(tableName, sequenceList);

                if (tablemetadata.Id > 0)
                {
                    _context.Update(tablemetadata);
                }
                else
                {
                    _context.Add(tablemetadata);
                }

                await _context.SaveChangesAsync();
            }

            tableDataVM.ColumnList = columnList;
            tableDataVM.TableDataList = tableDataDict;
            tableDataVM.SequenceName = tablemetadata.SequenceName;
            tableDataVM.SortColumn = sortColumn;
            tableDataVM.SortDir = sortDir;

            var searchFieldsArray = JsonConvert.DeserializeObject<List<SearchFieldInfo>>(searchFields);

            foreach (var field in searchFieldsArray)
            {
                tableDataVM.SearchValues[field.Name] = field.Value;
            }

            return View(tableDataVM);
        }

        [HttpPost]
        public async Task<IActionResult> AddDataPost(string connectionName, string tableName, IEnumerable<string> dataFields)
        {
            var tableDataVM = new TableDataVM
            {
                TableList = await _util.GetTableList(connectionName),
                TableName = tableName,
                ConnectionName = connectionName
            };

            tableDataVM.TableGroups = await _util.GetTableGroups(tableDataVM.TableList);

            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);
            var connectionString = Util.GetConnectionString(customConnection);

            var columnList = new List<TableColumnInfo>();
            var tableDataDict = new PagedData { Data = new Dictionary<int, Row>() };
            columnList = await _util.GetColumnInfo(connectionName, tableName);

            var tablemetadata = await _context.TableMetadata.SingleOrDefaultAsync(x => x.TableName == tableName);

            tableDataVM.ColumnList = columnList;
            tableDataVM.TableDataList = tableDataDict;
            tableDataVM.SequenceName = tablemetadata.SequenceName;

            var columnListStmt = "";
            var builderColumn = new System.Text.StringBuilder();
            builderColumn.Append(columnListStmt);

            foreach (var column in columnList)
            {
                builderColumn.Append(column.Name + ", ");
            }
            columnListStmt = builderColumn.ToString();

            columnListStmt = columnListStmt.TrimEnd(' ').TrimEnd(',');

            var valueListStmt = "";

            var dataFieldArray = dataFields.ToArray();
            var builder = new System.Text.StringBuilder();
            builder.Append(valueListStmt);

            for (int i = 0; i < dataFieldArray.Count(); i++)
            {
                if (columnList[i].IsPrimaryKey)
                {
                    builder.Append(tablemetadata.SequenceName + ".NEXTVAL, ");
                }
                else
                {
                    if (columnList[i].DataType.Equals("DATE"))
                    {
                        builder.Append("TO_DATE('" + dataFieldArray[i] + "','dd.mm.yyyy HH24:MI:SS'), ");
                    }
                    else
                    {
                        builder.Append("'" + dataFieldArray[i] + "', ");
                    }
                }
            }

            valueListStmt = builder.ToString();

            valueListStmt = valueListStmt.TrimEnd(' ').TrimEnd(',');

            var insertSqlStmt = "insert into " + tableName + " (" + columnListStmt + ") values (" + valueListStmt + ")";

            var sessionHistorySql = new SessionSqlHistory
            {
                EventDate = DateTime.Now,
                SqlText = insertSqlStmt,
                BasicSqlText = insertSqlStmt
            };

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                using (var cmd = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = insertSqlStmt,
                    CommandType = CommandType.Text
                })
                {
                    var result = cmd.ExecuteNonQuery();
                }
            }

            await _context.SessionSqlHistory.AddAsync(sessionHistorySql);
            await _context.SaveChangesAsync();

            return View("AddData", tableDataVM);
        }
    }
}