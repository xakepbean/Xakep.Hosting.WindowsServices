Use

```cs

        public static void Main(string[] args)
        {
            ServiceUtils.Run(args, (arg) => BuildWebHost(arg).Run(), Service =>
               {
                   Service.ServiceName = "netcorehostservice";
                   Service.DisplayName = "sample netcore host service";
                   Service.Description = "sample netcore host service Description";
                   Service.StartType = StartType.Auto;
                   Service.Account = AccountType.LocalSystem;
               });
        }
        public static IWebHost BuildWebHost(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>()
               .Build();

```

Optional parameters

dotnet xxx.dll  Start             start the service

dotnet xxx.dll  Stop              stop the service

dotnet xxx.dll  Reset             reset the service

dotnet xxx.dll  Install           install the service

dotnet xxx.dll  UnInstall         uninstall the service

No parameters

   ```cs
   execute IWebHost.Run();
   ```

