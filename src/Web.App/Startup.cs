using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Web.App.BusinessLayer;
using Web.App.Data;
using Web.App.Models;
using Web.App.Services;

namespace Web.App
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            // Add Database Initializer

            var dashcoreDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"DashCore");

            if (!File.Exists(dashcoreDir))
            {
                Directory.CreateDirectory(dashcoreDir);
            }

            var sqlitePath = "Data Source=" + Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"DashCore\CustomConnection.db");

            services.AddDbContext<CustomConnectionContext>(options =>
                    options.UseSqlite(sqlitePath));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<CustomConnectionContext>()
                .AddDefaultTokenProviders();
            services.AddScoped<IDbInitializer, DbInitializer>();
            services.AddScoped<IUtil,Util>();
            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddSingleton<IConfiguration>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IDbInitializer dbInitializer)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<CustomConnectionContext>();
                context.Database.Migrate();
                context.Database.ExecuteSqlCommand("delete from SessionSqlHistory");
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            //Generate EF Core Seed Data
            // dbInitializer.Initialize();

            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".tag"] = "text/plain";
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
