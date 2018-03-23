using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class TablesMenuVM
    {
        public Dictionary<string, List<string>> TableGroups { get; set; }
        public string ConnectionName { get; set; }
    }
}
