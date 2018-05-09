using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class EditBlobFieldVM
    {
        public string ConnectionName { get; set; }
        public string Table { get; set; }
        public string ColumnName { get; set; }
        public string PrimaryKeyColumn { get; set; }
        public string PrimaryKeyValue { get; set; }
        public byte[] Value { get; set; }
        public string ImgDataURL { get; internal set; }
    }
}
