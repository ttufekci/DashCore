using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class CustomConnectionContext : DbContext
    {
        public CustomConnectionContext(DbContextOptions<CustomConnectionContext> options)
            : base(options)
        {
        }

        public DbSet<Web.App.Models.CustomConnection> CustomConnection { get; set; }
        public DbSet<Web.App.Models.TableMetadata> TableMetadata { get; set; }
        public DbSet<SessionSqlHistory> SessionSqlHistory { get; set; }

    }
}
