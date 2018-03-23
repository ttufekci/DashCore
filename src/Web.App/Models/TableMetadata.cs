using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class TableMetadata
    {
        [Key]
        public long Id { get; set; }
        public string TableName { get; set; }
        public string SequenceName { get; set; }
    }
}
