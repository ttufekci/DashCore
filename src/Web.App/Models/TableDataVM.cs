using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class TableDataVM
    {
        public string SortColumn { get; internal set; } = "";
        public string SortDir { get; internal set; } = "";
        public List<string> TableList { get; set; }
        public string TableName { get; set; }
        public string ConnectionName { get; set; }
        public List<TableColumnInfo> ColumnList { get; set; }
        public PagedData TableDataList { get; set; }
        public string SequenceName { get; set; }
        public Row RowData { get; set; }
        public string TableColumnInfosJson { get; set; }
        public Dictionary<string, string> SearchValues { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<string>> TableGroups { get; set; } = new Dictionary<string, List<string>>();
        public string ForeignTableColumn { get; internal set; }
        public string ParentColumn { get; internal set; }
        public int PagerStart { get; set; }
        public string SearchFields { get; set; }
    }
}
