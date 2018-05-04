using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Web.App.Models;

namespace Web.App.BusinessLayer
{
    public class Util : IUtil
    {
        private readonly CustomConnectionContext _context;

        public static Dictionary<string, List<string>> ConnectionTableList { get; set; } = new Dictionary<string, List<string>>();

        public static Dictionary<string, List<TableColumnInfo>> ConnectionColumnInfo { get; set; } = new Dictionary<string, List<TableColumnInfo>>();

        public Util(CustomConnectionContext context)
        {
            _context = context;
        }

        public static string GetConnectionString(CustomConnection connection)
        {
            var host = connection.Host;
            var port = connection.Port;
            var SID = connection.SID;
            var user = connection.Username;
            var password = connection.Password;

            return $"DATA SOURCE=(DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = {host})(PORT = {port}))(CONNECT_DATA = (SERVER = DEDICATED) (SERVICE_NAME = {SID})));PERSIST SECURITY INFO=True;USER ID={user};Password={password}";
        }

        public static int FindPagerStartForLastPage(int pageCount)
        {
            return FindPagerStart(pageCount-1);
        }

        public static int FindPagerStartForPreviousPage(int page)
        {
            return FindPagerStart(page-1);
        }

        public static int FindPagerStartForFirstPage()
        {
            return 1;
        }

        public static int FindPagerStartForNextPage(int page)
        {
            return FindPagerStart(page + 1);
        }

        public static int FindPagerStart(int page)
        {
            if (page % 10 == 0)
                return (page + 1);

            return (page / 10) * 10 + 1;
        }

        public static string FindBestMatch(string tableName, List<string> sequenceList)
        {
            var insideSequence = sequenceList.FirstOrDefault(x => x.EndsWith(tableName));
            if (insideSequence != null) return insideSequence;

            var insideSequenceWithoutHyphen = sequenceList.FirstOrDefault(x => x.EndsWith(tableName.Replace("_", "")));
            if (insideSequenceWithoutHyphen != null) return insideSequenceWithoutHyphen;

            var tableNameWithoutPrefix = tableName.Substring(tableName.IndexOf('_') + 1);

            var insideSequenceWithoutPrefix = sequenceList.FirstOrDefault(x => x.EndsWith(tableNameWithoutPrefix));
            if (insideSequenceWithoutPrefix != null) return insideSequenceWithoutPrefix;

            var tableNameWithoutPrefixWithoutHyphen = tableName.Substring(tableName.IndexOf('_') + 1).Replace("_", "");

            var insideSequenceWithoutPrefixWithoutHyphen = sequenceList.FirstOrDefault(x => x.EndsWith(tableNameWithoutPrefixWithoutHyphen));
            if (insideSequenceWithoutPrefixWithoutHyphen != null) return insideSequenceWithoutPrefixWithoutHyphen;

            return "undefined";
        }

        public async Task<List<string>> GetTableList(CustomConnection customConnection)
        {
            var connectionString = GetConnectionString(customConnection);

            if (ConnectionTableList.ContainsKey(customConnection.Name)) return ConnectionTableList[customConnection.Name];

            var tableList = new List<string>();

            const string tableListSql = "select TABLE_NAME from user_tables order by TABLE_NAME";

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                using (var cmd = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = tableListSql,
                    CommandType = CommandType.Text
                })
                {
                    var dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        tableList.Add(dr.GetString(0));
                    }
                }
            }

            ConnectionTableList[customConnection.Name] = tableList;

            return tableList;
        }

        public async Task<List<string>> GetTableList(string connectionName)
        {
            if (ConnectionTableList.ContainsKey(connectionName)) return ConnectionTableList[connectionName];

            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var tableList = new List<string>();

            const string tableListSql = "select TABLE_NAME from user_tables order by TABLE_NAME";

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                using (var cmd = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = tableListSql,
                    CommandType = CommandType.Text
                })
                {
                    var dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        tableList.Add(dr.GetString(0));
                    }
                }
            }

            ConnectionTableList[connectionName] = tableList;

            return tableList;
        }

        public async Task<List<TableColumnInfo>> GetColumnInfo(string connectionName, string tableName)
        {
            var columnInfoKey = connectionName + ":" + tableName;

            if (ConnectionColumnInfo.ContainsKey(columnInfoKey)) return ConnectionColumnInfo[columnInfoKey];

            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var columnList = new List<TableColumnInfo>();

            const string columnListSql = "select COLUMN_NAME, DATA_TYPE from user_tab_columns where TABLE_NAME = :tablename order by column_id";

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();

                var primaryColumnList = new List<string>();
                var foreignKeyColumnDict = new Dictionary<string, ForeignKeyInfo>();

                var primaryForeignColumns = "SELECT ac.table_name, column_name, position, ac.constraint_name, constraint_type, " +
                                     "(SELECT ac2.table_name FROM all_constraints ac2 WHERE AC2.CONSTRAINT_NAME = AC.R_CONSTRAINT_NAME) fK_to_table, " +
                                     "(SELECT ac3.column_name FROM all_cons_columns ac3 WHERE AC3.CONSTRAINT_NAME = AC.R_CONSTRAINT_NAME) fK_to_table_column " +
                                     "FROM all_cons_columns acc, all_constraints ac " +
                                     "WHERE     acc.constraint_name = ac.constraint_name " +
                                        "AND acc.table_name = ac.table_name " +
                                        "AND CONSTRAINT_TYPE IN('P', 'R') " +
                                        "AND ac.table_name = '" + tableName + "' " +
                                        "ORDER BY table_name, constraint_type, position";

                using (var cmdPrimaryForeignColumns = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = primaryForeignColumns,
                    CommandType = CommandType.Text
                })
                {
                    var drPrimForeignColumns = cmdPrimaryForeignColumns.ExecuteReader();
                    while (drPrimForeignColumns.Read())
                    {
                        var constraintType = drPrimForeignColumns.GetString(4);

                        if (constraintType == "P")
                        {
                            primaryColumnList.Add(drPrimForeignColumns.GetString(1));
                        }
                        else
                        {
                            var name = drPrimForeignColumns.GetString(1);
                            foreignKeyColumnDict[name] = new ForeignKeyInfo
                            {
                                Name = drPrimForeignColumns.GetString(1),
                                ForeignKeyTable = drPrimForeignColumns.GetString(5),
                                ForeignKeyColumn = drPrimForeignColumns.GetString(6)
                            };
                        }
                    }

                    using (var cmd = new OracleCommand
                    {
                        Connection = oconn,
                        CommandText = columnListSql,
                        CommandType = CommandType.Text
                    })
                    {
                        cmd.Parameters.Add("tablename", tableName);

                        var dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            var columnName = dr.GetString(0);
                            var isPrimary = false;
                            var isForeignKey = false;

                            if (primaryColumnList.Contains(columnName))
                            {
                                isPrimary = true;
                            }

                            var tableColumnInfo = new TableColumnInfo
                            {
                                Name = dr.GetString(0),
                                DataType = dr.GetString(1),
                                IsPrimaryKey = isPrimary,
                                IsForeignKey = isForeignKey
                            };

                            if (foreignKeyColumnDict.ContainsKey(columnName))
                            {
                                isForeignKey = true;
                                var foreignKeyInfo = foreignKeyColumnDict[columnName];
                                tableColumnInfo.IsForeignKey = true;
                                tableColumnInfo.ForeignTable = foreignKeyInfo.ForeignKeyTable;
                                tableColumnInfo.ForeignTableKeyColumn = foreignKeyInfo.ForeignKeyColumn;
                            }

                            columnList.Add(tableColumnInfo);
                        }
                    }
                }
            }

            ConnectionColumnInfo[columnInfoKey] = columnList;

            return columnList;
        }

        public async Task<PagedData> GetTableDataList(string connectionName, string tableName, int page = 0)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var columnList = await GetColumnInfo(connectionName, tableName);

            return await GetTableDataList(columnList, connectionName, tableName, page);
        }

        public async Task<PagedData> GetTableDataList(List<TableColumnInfo> columnList, string connectionName, string tableName, int page = 0)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var tableDataListCountSql = @"select count(*) from " + tableName;

            var pagedData = new PagedData
            {
                Page = page
            };

            decimal count = 0;

            var belowRowIndex = 1;
            var topRowIndex = 1 * pagedData.PageSize;

            if (page > 0)
            {
                belowRowIndex = page * pagedData.PageSize + 1;
                topRowIndex = (page + 1) * pagedData.PageSize;
            }

            var tableDataListSql = @"select * from ( " +
                                      "select mt.*, " +
                                      "row_number() over (order by ROWID) rn " +
                                      "from " + tableName + @" mt) " +
                                      "where rn between " + belowRowIndex + " and " + topRowIndex + " order by rn";

            var sessionSqlHistory = new SessionSqlHistory
            {
                EventDate = DateTime.Now,
                SqlText = tableDataListSql,
                BasicSqlText = "select * from " + tableName
            };

            var myRowDict = new Dictionary<int, Row>();

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();

                using (var cmdCount = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = tableDataListCountSql,
                    CommandType = CommandType.Text
                })
                {
                    count = (decimal)cmdCount.ExecuteScalar();

                    oconn.Open();
                    using (var cmd = new OracleCommand
                    {
                        Connection = oconn,
                        CommandText = tableDataListSql,
                        CommandType = CommandType.Text
                    })
                    {
                        var dr = cmd.ExecuteReader();

                        var viewRowId = 0;

                        while (dr.Read())
                        {
                            var row = new Row();

                            var rowData = new List<TableColumnInfo>();

                            for (int i = 0; i < columnList.Count; i++)
                            {
                                var columnInfo = columnList[i];

                                var tableColumnInfo = new TableColumnInfo
                                {
                                    DataType = columnInfo.DataType,
                                    IsPrimaryKey = columnInfo.IsPrimaryKey,
                                    Name = columnInfo.Name
                                };

                                if (dr.GetValue(i) != DBNull.Value)
                                {
                                    tableColumnInfo.Value = dr.GetValue(i).ToString();
                                }
                                else
                                {
                                    tableColumnInfo.Value = "";
                                }

                                if (tableColumnInfo.IsPrimaryKey)
                                {
                                    row.PrimaryKey = string.IsNullOrEmpty(row.PrimaryKey) ? tableColumnInfo.Value : row.PrimaryKey + ";" + tableColumnInfo.Value;
                                }

                                rowData.Add(tableColumnInfo);
                            }

                            row.TableColumnInfos = rowData;
                            row.TableColumnInfosJson = JsonConvert.SerializeObject(rowData);

                            myRowDict.Add(viewRowId++, row);
                        }
                    }
                }
            }

            await _context.SessionSqlHistory.AddAsync(sessionSqlHistory);
            await _context.SaveChangesAsync();

            pagedData.Data = myRowDict;
            pagedData.Total = Convert.ToInt32(count);

            pagedData.PageCount = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(count) / (Convert.ToDouble(pagedData.PageSize))));

            return pagedData;
        }

        public async Task<PagedData> GetTableDataListSearch(string connectionName, string tableName, int page, string searchColumn, string searchValue)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var columnList = await GetColumnInfo(connectionName, tableName);

            var tableDataListCountSql = @"select count(*) from " + tableName + " where " + searchColumn + " like '" + searchValue + "%'";

            var pagedData = new PagedData
            {
                Page = page
            };

            decimal count = 0;

            var belowRowIndex = 1;
            var topRowIndex = 1 * pagedData.PageSize;

            if (page > 0)
            {
                belowRowIndex = page * pagedData.PageSize + 1;
                topRowIndex = (page + 1) * pagedData.PageSize;
            }

            var tableDataListSql = @"select * from ( " +
                                      "select mt.*, " +
                                      "row_number() over (order by ROWID) rn " +
                                      "from " + tableName + @" mt where " + searchColumn + " like '" + searchValue + "%') " +
                                      "where rn between " + belowRowIndex + " and " + topRowIndex + " order by rn";

            var sessionSqlHistory = new SessionSqlHistory
            {
                EventDate = DateTime.Now,
                SqlText = tableDataListSql,
                BasicSqlText = "select * from " + tableName + " where " + searchColumn + " like '" + searchValue + "%'"
            };  

            var myRowDict = new Dictionary<int, Row>();

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                using (var cmdCount = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = tableDataListCountSql,
                    CommandType = CommandType.Text
                })
                {
                    count = (decimal)cmdCount.ExecuteScalar();

                    oconn.Open();
                    using (var cmd = new OracleCommand
                    {
                        Connection = oconn,
                        CommandText = tableDataListSql,
                        CommandType = CommandType.Text
                    })
                    {
                        var dr = cmd.ExecuteReader();

                        var viewRowId = 0;

                        while (dr.Read())
                        {
                            var row = new Row();

                            var rowData = new List<TableColumnInfo>();

                            for (int i = 0; i < columnList.Count; i++)
                            {
                                var columnInfo = columnList[i];

                                var tableColumnInfo = new TableColumnInfo
                                {
                                    DataType = columnInfo.DataType,
                                    IsPrimaryKey = columnInfo.IsPrimaryKey,
                                    Name = columnInfo.Name
                                };

                                if (dr.GetValue(i) != DBNull.Value)
                                {
                                    tableColumnInfo.Value = dr.GetValue(i).ToString();
                                }
                                else
                                {
                                    tableColumnInfo.Value = "";
                                }

                                if (tableColumnInfo.IsPrimaryKey)
                                {
                                    row.PrimaryKey = string.IsNullOrEmpty(row.PrimaryKey) ? tableColumnInfo.Value : row.PrimaryKey + ";" + tableColumnInfo.Value;
                                }

                                rowData.Add(tableColumnInfo);
                            }

                            row.TableColumnInfos = rowData;
                            row.TableColumnInfosJson = JsonConvert.SerializeObject(rowData);

                            myRowDict.Add(viewRowId++, row);
                        }
                    }
                }
            }

            await _context.SessionSqlHistory.AddAsync(sessionSqlHistory);
            await _context.SaveChangesAsync();

            pagedData.Data = myRowDict;
            pagedData.Total = Convert.ToInt32(count);

            pagedData.PageCount = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(count) / (Convert.ToDouble(pagedData.PageSize))));

            return pagedData;
        }

        public async Task<PagedData> GetTableDataListSearch(string connectionName, string tableName, int page, int pageSize, List<SearchFieldInfo> searchFields, string sortColumn, string sortDir, string sortColumnDataType)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var columnList = await GetColumnInfo(connectionName, tableName);

            string whereStmt = "";

            foreach(var field in searchFields)
            {
                whereStmt += field.Name + " like '" + field.Value + "%' and ";
            }

            if (!string.IsNullOrEmpty(whereStmt))
            {
                whereStmt = " where " + whereStmt.TrimEnd(' ', 'a', 'n', 'd', ' ');
            }

            var orderStmt = "";

            if (!string.IsNullOrEmpty(sortColumn))
            {
                orderStmt = " order by " + sortColumn + " " + sortDir;
            }
            else
            {
                orderStmt = " order by ROWID ";
            }

            var tableDataListCountSql = @"select count(*) from " + tableName + whereStmt;

            var pagedData = new PagedData
            {
                Page = page,
                PageSize = pageSize
            };

            decimal count = 0;

            var belowRowIndex = 1;
            var topRowIndex = 1 * pagedData.PageSize;

            if (page > 0)
            {
                belowRowIndex = page * pagedData.PageSize + 1;
                topRowIndex = (page + 1) * pagedData.PageSize;
            }

            var tableDataListSql = @"select * from ( " +
                                      "select mt.*, " +
                                      "row_number() over (" + orderStmt + ") rn " +
                                      "from " + tableName + @" mt " + whereStmt + ") " +
                                      "where rn between " + belowRowIndex + " and " + topRowIndex + (string.IsNullOrEmpty(orderStmt) ? " order by rn" : "");

            var sessionSqlHistory = new SessionSqlHistory
            {
                EventDate = DateTime.Now,
                SqlText = tableDataListSql,
                BasicSqlText = "select * from " + tableName + whereStmt + (string.IsNullOrEmpty(orderStmt) ? "" : orderStmt)
            };

            var myRowDict = new Dictionary<int, Row>();

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();
                using (var cmdCount = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = tableDataListCountSql,
                    CommandType = CommandType.Text
                })
                {
                    count = (decimal)cmdCount.ExecuteScalar();

                    oconn.Open();
                    using (var cmd = new OracleCommand
                    {
                        Connection = oconn,
                        CommandText = tableDataListSql,
                        CommandType = CommandType.Text
                    })
                    {
                        var dr = cmd.ExecuteReader();

                        var viewRowId = 0;

                        while (dr.Read())
                        {
                            var row = new Row();

                            var rowData = new List<TableColumnInfo>();

                            for (int i = 0; i < columnList.Count; i++)
                            {
                                var columnInfo = columnList[i];

                                var tableColumnInfo = new TableColumnInfo
                                {
                                    DataType = columnInfo.DataType,
                                    IsPrimaryKey = columnInfo.IsPrimaryKey,
                                    Name = columnInfo.Name
                                };

                                if (dr.GetValue(i) != DBNull.Value)
                                {
                                    tableColumnInfo.Value = dr.GetValue(i).ToString();
                                }
                                else
                                {
                                    tableColumnInfo.Value = "";
                                }

                                if (tableColumnInfo.IsPrimaryKey)
                                {
                                    row.PrimaryKey = string.IsNullOrEmpty(row.PrimaryKey) ? tableColumnInfo.Value : row.PrimaryKey + ";" + tableColumnInfo.Value;
                                }

                                rowData.Add(tableColumnInfo);
                            }

                            row.TableColumnInfos = rowData;
                            row.TableColumnInfosJson = JsonConvert.SerializeObject(rowData);

                            myRowDict.Add(viewRowId++, row);
                        }
                    }
                }
            }

            await _context.SessionSqlHistory.AddAsync(sessionSqlHistory);
            await _context.SaveChangesAsync();

            pagedData.Data = myRowDict;
            pagedData.Total = Convert.ToInt32(count);

            pagedData.PageCount = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(count) / (Convert.ToDouble(pagedData.PageSize))));

            return pagedData;
        }

        public async Task<List<string>> GetSequenceList(CustomConnection customConnection)
        {
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

        public async Task<List<string>> GetSequenceList(string connectionName)
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

        public async Task<string> FindForeignDescription(OracleConnection oconn, string connectionName, string foreignTable, string foreignTableKeyColumn, string keyValue)
        {
            var findForeignRowSql = "select * from " + foreignTable + " where " + foreignTableKeyColumn + " = '" + keyValue + "'";
            var foreignDesc = "";

            using (var cmdForeign = new OracleCommand
            {
                Connection = oconn,
                CommandText = findForeignRowSql,
                CommandType = CommandType.Text
            })
            {
                var columnListForeign = await GetColumnInfo(connectionName, foreignTable);
                var drForeign = cmdForeign.ExecuteReader();
                var foreignCnt = 0;

                while (drForeign.Read())
                {
                    foreach (var item in columnListForeign)
                    {
                        if (drForeign.GetValue(foreignCnt) != DBNull.Value)
                        {
                            var column = item.Name;
                            foreignDesc += column + ":" + drForeign.GetValue(foreignCnt).ToString() + "   ";
                        }
                        foreignCnt++;
                    }
                }
            }

            return foreignDesc.Trim();
        }

        public async Task<Row> GetRowData(string connectionName, string tableName, string primaryKey, string tableColumnInfosJson)
        {
            var customConnection = await _context.CustomConnection.SingleOrDefaultAsync(x => x.Name == connectionName);

            var connectionString = Util.GetConnectionString(customConnection);

            var columnList = await GetColumnInfo(connectionName, tableName);

            var whereStmt = "";           

            if (primaryKey.Contains(";"))
            {
                var primkeys = primaryKey.Split(';');

                var j = 0;

                foreach (var primkey in primkeys)
                {                    
                    for (int i=j; i < columnList.Count; i++)
                    {
                        if (columnList[i].IsPrimaryKey)
                        {
                            if (columnList[i].DataType == "DATE")
                            {
                                whereStmt += columnList[i].Name + " = TO_DATE('" + primkey + "','dd.mm.yyyy HH24:MI:SS') and ";
                            }
                            else
                            {
                                whereStmt += columnList[i].Name + " = '" + primkey + "' and ";
                            }

                            j = i + 1;
                            goto Outer;
                        }
                    }

                    Outer:
                        continue;
                }

                whereStmt = whereStmt.TrimEnd(' ').TrimEnd('d').TrimEnd('n').TrimEnd('a');
            }
            else
            {
                for (int i = 0; i < columnList.Count; i++)
                {
                    if (columnList[i].IsPrimaryKey)
                    {
                        if (columnList[i].DataType == "DATE")
                        {
                            whereStmt += columnList[i].Name + " = TO_DATE('" + primaryKey + "','dd.mm.yyyy HH24:MI:SS') and ";
                        }
                        else
                        {
                            whereStmt += columnList[i].Name + " = '" + primaryKey + "' and ";
                        }
                        break;
                    }
                }

                whereStmt = whereStmt.TrimEnd(' ').TrimEnd('d').TrimEnd('n').TrimEnd('a');
            }

            var sqlStmt = "select * from " + tableName + " where " + whereStmt;

            if (string.IsNullOrEmpty(primaryKey))
            {
                var whereColumnListStmt = "";

                var oldColumnList = JsonConvert.DeserializeObject<List<TableColumnInfo>>(tableColumnInfosJson).ToArray();
                var builder = new System.Text.StringBuilder();
                builder.Append(whereColumnListStmt);

                for (int j = 0; j < columnList.Count(); j++)
                {
                    if (string.IsNullOrEmpty(oldColumnList[j].Value))
                    {
                        builder.Append(columnList[j].Name + " is null and ");
                    }
                    else
                    {
                        builder.Append(columnList[j].Name + "='" + oldColumnList[j].Value + "' and ");
                    }
                }
                whereColumnListStmt = builder.ToString();

                whereColumnListStmt = whereColumnListStmt.TrimEnd(' ', 'd', 'n', 'a');

                sqlStmt = "select * from " + tableName + " where " + whereColumnListStmt;
            }

            var sessionSqlHistory = new SessionSqlHistory
            {
                EventDate = DateTime.Now,
                SqlText = sqlStmt,
                BasicSqlText = sqlStmt
            };

            var row = new Row();

            using (var oconn = new OracleConnection(connectionString))
            {
                oconn.Open();

                using (var cmd = new OracleCommand
                {
                    Connection = oconn,
                    CommandText = sqlStmt,
                    CommandType = CommandType.Text
                })
                {
                    var dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        var rowData = new List<TableColumnInfo>();

                        for (int i = 0; i < columnList.Count; i++)
                        {
                            var columnInfo = columnList[i];

                            if (dr.GetValue(i) != DBNull.Value)
                            {
                                columnInfo.Value = dr.GetValue(i).ToString();
                                columnInfo.OldValue = dr.GetValue(i).ToString();
                            }
                            else
                            {
                                columnInfo.Value = "";
                            }

                            if (columnInfo.IsPrimaryKey)
                            {
                                row.PrimaryKey = string.IsNullOrEmpty(row.PrimaryKey) ? columnInfo.Value : row.PrimaryKey + ";" + columnInfo.Value;
                            }

                            if (!string.IsNullOrEmpty(columnInfo.Value) && columnInfo.IsForeignKey)
                            {                                
                                var foreignTable = columnInfo.ForeignTable;
                                var findForeignRowSql = "select * from " + foreignTable + " where " + columnInfo.ForeignTableKeyColumn + " = '" + columnInfo.Value + "'";
                                var foreignDesc = await FindForeignDescription(oconn, connectionName, foreignTable, columnInfo.ForeignTableKeyColumn, columnInfo.Value);
                                columnInfo.ForeignDescription = foreignDesc.Trim();
                            }

                            rowData.Add(columnInfo);
                        }

                        row.TableColumnInfos = rowData;

                        break;
                    }
                }
            }

            await _context.SessionSqlHistory.AddAsync(sessionSqlHistory);
            await _context.SaveChangesAsync();

            return row;
        }

        public async Task<Dictionary<string, List<string>>> GetTableGroups(List<string> tableList)
        {
            var result = new Dictionary<string, List<string>>();

            for (int i = 0; i < tableList.Count(); i++)
            {
                var table = tableList[i];
                var separatorIndx = table.IndexOf("_");

                if (separatorIndx >= 0)
                {
                    var prefix = table.Substring(0, separatorIndx);

                    if (!result.ContainsKey(prefix))
                    {
                        result[prefix] = new List<string>();
                    }
                }
            }

            result["Ungrouped"] = new List<string>();

            for (int i = 0; i < tableList.Count(); i++)
            {
                var table = tableList[i];
                var separatorIndx = table.IndexOf("_");

                if (separatorIndx >= 0)
                {
                    var prefix = table.Substring(0, separatorIndx);

                    if (result.ContainsKey(prefix))
                    {
                        result[prefix].Add(table);
                    }
                }
                else
                {
                    result["Ungrouped"].Add(table);
                }
            }

            return result;
        }
    }
}
