using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class Row
    {
        public List<TableColumnInfo> TableColumnInfos { get; set; }
        public string PrimaryKey { get; set; }
        public string TableColumnInfosJson { get; set; }
        public string RowDescription { get; set; }
    }
}
