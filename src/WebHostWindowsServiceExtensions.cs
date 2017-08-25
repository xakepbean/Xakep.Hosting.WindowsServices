using DasMulli.Win32.ServiceUtils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Xakep.Hosting.WindowsServices
{
    /// <summary>
    ///     Extensions to <see cref="IWebHost"/> for hosting inside a Windows service.
    /// </summary>
    public static class WebHostWindowsServiceExtensions
    {

        /// <summary>
        ///     Runs the specified web application inside a Windows service and blocks until the service is stopped.
        /// </summary>
        /// <param name="host">An instance of the <see cref="IWebHost"/> to host in the Windows service.</param>
        /// <example>
        ///     This example shows how to use <see cref="RunAsService"/>.
        ///     <code>
        ///         public class Program
        ///         {
        ///                 public static void Main(string[] args)
        ///                 {
        ///                     BuildWebHost().RunAsService(args);
        ///                 }
        ///                 public static IWebHost BuildWebHost() =>
        ///                    WebHost.CreateDefaultBuilder()
        ///                         .UseStartup<Startup>()
        ///                         .Build();
        ///         }
        ///     </code>
        /// </example>
        public static void RunAsService(this IWebHost host, string[] args, Action<Settings> SetService = null)
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
                        Manage(args[0].ToLower(), InitSettings(host, SetService));
                        break;
                    case "run":
                        Run(InitSettings(host, SetService));
                        break;
                    default:
                        Console.WriteLine("Invalid parameter");
                        Console.WriteLine();
                        Console.WriteLine();
                        ShowHelp();
                        break;
                }
            }
            else
            {
                ShowHelp();
                host.RunAsync().GetAwaiter().GetResult();
            }
        }

        public static void Run(Settings Set)
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
                ? $"{binaryPath} \"{Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, PlatformServices.Default.Application.ApplicationName + ".dll")}\" run"
                : $"{binaryPath} run";

            new Win32ServiceManager().CreateService(
                Set.ServiceName,
                Set.DisplayName,
                Set.Description,
               binaryPath,
                cred,
                autoStart: Set.StartType == StartType.Auto,
                startImmediately: Set.StartType == StartType.Auto,
                errorSeverity: ErrorSeverity.Normal);
            Console.WriteLine($@"Successfully registered and started service ""{Set.ServiceName}"" (""{Set.Description}"")");
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

        private static Settings InitSettings(IWebHost host,Action<Settings> SetService = null)
        {
            var Settings = new Settings();
            Settings.ServiceName = "Xakep.Hosting.WindowsServices";
            Settings.Account = AccountType.NetworkService;
            Settings.StartType = StartType.Auto;
            Settings.DisplayName = Settings.ServiceName;
            Settings.Description = ".net core services";
            Settings.WriteLog = true;
            Settings.LogPath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Servicelog.txt");
            if (SetService != null)
                SetService(Settings);
            Settings.Service = new WebHostService(host, Settings.ServiceName, Settings.WriteLog, Settings.LogPath);
            return Settings;
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Optional parameters");
            Console.WriteLine("     Start             start the service");
            Console.WriteLine("     Stop              stop the service");
            Console.WriteLine("     Reset             reset the service");
            Console.WriteLine("     Install           install the service");
            Console.WriteLine("     UnInstall         uninstall the service ");
            Console.WriteLine();
            Console.WriteLine();
        }

        #region Host
        private static async Task RunAsync(this IWebHost host, CancellationToken token = default(CancellationToken))
        {
            // Wait for token shutdown if it can be canceled
            if (token.CanBeCanceled)
            {
                await host.RunAsync(token, shutdownMessage: null);
                return;
            }

            // If token cannot be canceled, attach Ctrl+C and SIGTERM shutdown
            var done = new ManualResetEventSlim(false);
            using (var cts = new CancellationTokenSource())
            {
                AttachCtrlcSigtermShutdown(cts, done, shutdownMessage: "Application is shutting down...");

                await host.RunAsync(cts.Token, "Application started. Press Ctrl+C to shut down.");
                done.Set();
            }
        }

        private static async Task RunAsync(this IWebHost host, CancellationToken token, string shutdownMessage)
        {
            using (host)
            {
                await host.StartAsync(token);

                var hostingEnvironment = host.Services.GetService<IHostingEnvironment>();
                var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

                Console.WriteLine($"Hosting environment: {hostingEnvironment.EnvironmentName}");
                Console.WriteLine($"Content root path: {hostingEnvironment.ContentRootPath}");

                var serverAddresses = host.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
                if (serverAddresses != null)
                {
                    foreach (var address in serverAddresses)
                    {
                        Console.WriteLine($"Now listening on: {address}");
                    }
                }

                if (!string.IsNullOrEmpty(shutdownMessage))
                {
                    Console.WriteLine(shutdownMessage);
                }

                await host.WaitForTokenShutdownAsync(token);
            }
        }

        private static void AttachCtrlcSigtermShutdown(CancellationTokenSource cts, ManualResetEventSlim resetEvent, string shutdownMessage)
        {
            void Shutdown()
            {
                if (!cts.IsCancellationRequested)
                {
                    if (!string.IsNullOrEmpty(shutdownMessage))
                    {
                        Console.WriteLine(shutdownMessage);
                    }
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException) { }
                }

                // Wait on the given reset event
                resetEvent.Wait();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Shutdown();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Shutdown();
                // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                eventArgs.Cancel = true;
            };
        }

        private static async Task WaitForTokenShutdownAsync(this IWebHost host, CancellationToken token)
        {
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

            token.Register(state =>
            {
                ((IApplicationLifetime)state).StopApplication();
            },
            applicationLifetime);

            var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            applicationLifetime.ApplicationStopping.Register(obj =>
            {
                var tcs = (TaskCompletionSource<object>)obj;
                tcs.TrySetResult(null);
            }, waitForStop);

            await waitForStop.Task;

            // WebHost will use its default ShutdownTimeout if none is specified.
            await host.StopAsync();
        }

        #endregion

    }
}
