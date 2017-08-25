using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xakep.Hosting.WindowsServices;

namespace sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost().RunAsService(args, Service =>
             {
                 Service.ServiceName = "netcorehostservice";
                 Service.DisplayName = "sample netcore host service";
                 Service.Description = " sample netcore host service Description";
                 Service.StartType = StartType.Auto;
                 Service.Account = AccountType.LocalSystem;
             });
        }
        public static IWebHost BuildWebHost() =>
           WebHost.CreateDefaultBuilder()
               .UseStartup<Startup>()
               .Build();

    }
}
