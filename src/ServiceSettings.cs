using DasMulli.Win32.ServiceUtils;
using System;
using System.Collections.Generic;
using System.Text;

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

        internal string LogPath { get; set; }

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
