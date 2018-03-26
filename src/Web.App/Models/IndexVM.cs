using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class IndexVM
    {
        public IList<CustomConnection> Connections { get; set; }
        public string Version { get; set; }
    }
}
