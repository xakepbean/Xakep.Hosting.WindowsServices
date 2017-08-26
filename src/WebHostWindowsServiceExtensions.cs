using DasMulli.Win32.ServiceUtils;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Linq;

namespace Xakep.Hosting.WindowsServices
{
    public static class ServiceUtils
    {

        /// <summary>
        ///     Runs the specified web application inside a Windows service and blocks until the service is stopped.
        /// </summary>
        /// <example>
        ///     This example shows how to use <see cref="Run"/>.
        ///     <code>
        ///         public class Program
        ///         {
        ///                 public static void Main(string[] args)
        ///                 {
        ///                     ServiceUtils.Run(args, (arg) => BuildWebHost(arg).Run(), Service =>
        ///                        {
        ///                               Service.ServiceName = "netcorehostservice";
        ///                               Service.DisplayName = "sample netcore host service";
        ///                               Service.Description = "sample netcore host service Description";
        ///                               Service.StartType = StartType.Auto;
        ///                               Service.Account = AccountType.LocalSystem;
        ///                        });
        ///                 }
        ///                 public static IWebHost BuildWebHost(string[] args) =>
        ///                    WebHost.CreateDefaultBuilder(args)
        ///                         .UseStartup<Startup>()
        ///                         .Build();
        ///         }
        ///     </code>
        /// </example>
        public static void Run(string[] args, Action<string[]> RunHost, Action<Settings> SetService = null)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "start":
                    case "stop":
                    case "install":
                    case "uninstall":
                    case "reset":
                        Manage(args[0].ToLower(), InitSettings(args.Skip(1).ToArray(),SetService));
                        break;
                    case "srun":
                        Run(InitSettings(args.Skip(1).ToArray(), SetService));
                        break;
                    default:
                        Console.WriteLine("Invalid parameter");
                        Console.WriteLine();
                        Console.WriteLine();
                        ShowHelp();
                        RunHost(args);
                        break;
                }
            }
            else
            {
                ShowHelp();
                RunHost(args);
            }
        }

        private static void Run(Settings Set)
        {
            try
            {
                var serviceHost = new Win32ServiceHost(Set.Service);
                serviceHost.Run();
            }
            catch (Exception ex)
            {
                if (Set.WriteLog)
                    File.AppendAllText(Set.LogPath, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff")} {Set.ServiceName}({Set.DisplayName}) Started{Environment.NewLine}");
                Console.WriteLine(ex.Message);
            }
        }

        private static void Manage(string Command, Settings Set)
        {
            var Service = ServiceController.GetServices().Where(w => w.ServiceName == Set.ServiceName);
            if (Command == "install")
            {
                if (Service.Count() == 0)
                    Install(Set);
                else
                    Console.WriteLine($"Service {Set.ServiceName}({Set.DisplayName}) was already installed.");
            }
            else
            {
                if (Service.Count() > 0)
                {
                    switch (Command)
                    {
                        case "reset":
                            Stop(Service.First());
                            Start(Service.First());
                            break;
                        case "start":
                            Start(Service.First());
                            break;
                        case "stop":
                            Stop(Service.First());
                            break;
                        case "uninstall":
                            UnInstall(Service.First());
                            break;
                    }
                }
                else
                    Console.WriteLine($"Service {Set.ServiceName}({Set.DisplayName}) does not exist.");
            }
        }

        private static void Start(ServiceController Service)
        {
            if (!(Service.Status == ServiceControllerStatus.StartPending | Service.Status == ServiceControllerStatus.Running))
            {
                Service.Start();
                Service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(1000));
                Console.WriteLine($"Successfully started service {Service.DisplayName}({Service.ServiceName})");
            }
            else
            {
                Console.WriteLine($"Service {Service.DisplayName}({Service.ServiceName}) is already running or start is pending.");
            }
        }

        private static void Stop(ServiceController Service)
        {
            if (!(Service.Status == ServiceControllerStatus.Stopped | Service.Status == ServiceControllerStatus.StopPending))
            {
                Service.Stop();
                Service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(1000));
                Console.WriteLine($"Successfully stopped service {Service.DisplayName}({Service.ServiceName})");
            }
            else
            {
                Console.WriteLine($"Service {Service.DisplayName}({Service.ServiceName}) is already stopped or stop is pending.");
            }
        }

        private static void Install(Settings Set)
        {
            Win32ServiceCredentials cred = Win32ServiceCredentials.NetworkService;
            switch (Set.Account)
            {
                case AccountType.LocalService:
                    cred = Win32ServiceCredentials.LocalService;
                    break;
                case AccountType.LocalSystem:
                    cred = Win32ServiceCredentials.LocalSystem;
                    break;
            }

            var binaryPath = Process.GetCurrentProcess().MainModule.FileName;
            binaryPath = binaryPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase)
                ? $"{binaryPath} \"{Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, PlatformServices.Default.Application.ApplicationName + ".dll")}\" srun"
                : $"{binaryPath} srun";

            new Win32ServiceManager().CreateService(
                Set.ServiceName,
                Set.DisplayName,
                Set.Description,
               binaryPath,
                cred,
                autoStart: Set.StartType == StartType.Auto,
                startImmediately: Set.StartType == StartType.Auto,
                errorSeverity: ErrorSeverity.Normal);
            Console.WriteLine($@"Successfully registered and started service ""{Set.ServiceName}"" (""{Set.DisplayName}"")");
        }

        private static void UnInstall(ServiceController Service)
        {
            if (!(Service.Status == ServiceControllerStatus.Stopped || Service.Status == ServiceControllerStatus.StopPending))
            {
                Stop(Service);
            }
            new Win32ServiceManager().DeleteService(Service.ServiceName);
            Console.WriteLine($"Successfully uninstall service {Service.DisplayName}({Service.ServiceName})");
        }

        private static Settings InitSettings(string[] args, Action<Settings> SetService = null)
        {
            var Settings = new Settings();
            Settings.ServiceName = "Xakep.Hosting.WindowsServices";
            Settings.Account = AccountType.NetworkService;
            Settings.StartType = StartType.Auto;
            Settings.DisplayName = Settings.ServiceName;
            Settings.Description = ".net core services";
            Settings.WriteLog = true;
            Settings.Args = args;
            Settings.LogPath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Servicelog.txt");
            if (SetService != null)
                SetService(Settings);

            Settings.Service = new WebHostService(Settings);
            return Settings;
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Xakep.Hosting.WindowsServices Help");
            Console.WriteLine("Optional parameters");
            Console.WriteLine("     Start             start the service");
            Console.WriteLine("     Stop              stop the service");
            Console.WriteLine("     Reset             reset the service");
            Console.WriteLine("     Install           install the service");
            Console.WriteLine("     UnInstall         uninstall the service ");
            Console.WriteLine();
            Console.WriteLine();
        }

    }
}
