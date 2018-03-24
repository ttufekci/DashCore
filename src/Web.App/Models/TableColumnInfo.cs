using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class TableColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string Value { get; set; }
        public string OldValue { get; set; }
        public bool IsForeignKey { get; set; }
        public string ForeignTable { get; set; }
        public string ForeignTableKeyColumn { get; set; }
        public string ForeignDescription { get; set; }
        public bool Visible { get; set; } = true;
    }
}
