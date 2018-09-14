using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Web.App.Data;
using Web.App.Models;

namespace Web.App
{
    public class Program
    {
        public static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                // throw 
            }
        }

        public static void Main(string[] args)
        {
            var currentPId = Process.GetCurrentProcess().Id;

            foreach (Process PPath in Process.GetProcesses())
            {
                if (PPath.ProcessName.StartsWith("Web.App") && PPath.Id != currentPId)
                {
                    PPath.Kill();
                }
            }

            var host = BuildWebHost(args);           

            using (var scope = host.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                try
                {
                    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    var context = serviceProvider.GetRequiredService<CustomConnectionContext>();
                    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

                    DbInitializer.StaticInitialize(context, userManager, roleManager, configuration);
                }
                catch
                {
                   
                }
            }

            #if !DEBUG
            OpenBrowser("http://localhost:5001/");
            #endif

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls("http://localhost:5001")
                .Build();
    }
}
