using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class PagedData
    {
        public Dictionary<int, Row> Data { get; set; }
        public int PageCount { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; } = 10;
        public PagedData()
        {
        }
    }
}
