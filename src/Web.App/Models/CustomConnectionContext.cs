using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.App.Models
{
    public class CustomConnectionContext : IdentityDbContext<ApplicationUser,IdentityRole,string>
    {
        public CustomConnectionContext(DbContextOptions<CustomConnectionContext> options)
            : base(options)
        {
        }

        public DbSet<CustomConnection> CustomConnection { get; set; }
        public DbSet<TableMetadata> TableMetadata { get; set; }
        public DbSet<SessionSqlHistory> SessionSqlHistory { get; set; }
    }
}
