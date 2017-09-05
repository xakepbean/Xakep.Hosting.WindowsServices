using DasMulli.Win32.ServiceUtils;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.IO;

namespace Xakep.Hosting.WindowsServices
{
    public class Settings
    {
        public string ServiceName { get; set; }

        public string Description { get; set; }

        public string DisplayName { get; set; }

        public AccountType Account { get; set; }

        public StartType StartType { get; set; }

        public bool WriteLog { get; set; }

        internal string[] Args { get; set; }

        private string _LogsPath = string.Empty;
        private string LogsPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_LogsPath))
                {
                    var logpath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "logs");
                    if (!Directory.Exists(logpath))
                        Directory.CreateDirectory(logpath);
                    _LogsPath = logpath;
                }
                return _LogsPath;
            }
        }

        internal string LogPath {
            get {
                return Path.Combine(LogsPath, $"host_{DateTime.Now.ToString("yyyyMMdd")}.log");
            }
        }

        internal IWin32Service Service { get; set; }
    }

    public enum StartType
    {
        Auto,
        Manual,
        Disabled
    }

    public enum AccountType
    {
        LocalSystem,
        LocalService,
        NetworkService,
    }

}
