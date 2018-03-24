using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class SessionSqlHistory
    {
        [Key]
        public long Id { get; set; }
        public string SessionId { get; set; }
        [Display(Name = "Date")]
        public DateTime EventDate { get; set; }
        [Display(Name = "Raw Sql")]
        public string SqlText { get; set; }
        [Display(Name = "Basic Sql")]
        public string BasicSqlText { get; set; }
    }
}
