using DasMulli.Win32.ServiceUtils;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;

namespace Xakep.Hosting.WindowsServices
{
    public class WebHostService: IWin32Service
    {
        private IWebHost _host;
        private bool _writeLog = true;
        private string _logPath = string.Empty;

        public WebHostService(IWebHost host,string serviceName, bool writeLog, string logPath)
        {
            _host = host;
            _writeLog = writeLog;
            ServiceName = serviceName;
            _logPath = logPath;
        }

        public string ServiceName { get; set; }

        public void Start(string[] startupArguments, ServiceStoppedCallback serviceStoppedCallback)
        {
            try
            {
                if (_writeLog)
                    File.AppendAllText(_logPath, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff")} {ServiceName} Started{Environment.NewLine}");
                Console.WriteLine($"{ServiceName} Started");
                _host.Start();
            }
            catch (Exception)
            {
                Stop();
                serviceStoppedCallback();
            }
        }

        public void Stop()
        {
            Console.WriteLine($"{ServiceName} Stopped");
            _host.StopAsync();

            _host?.Dispose();
            if (_writeLog)
                File.AppendAllText(_logPath, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff")} {ServiceName} Stopped{Environment.NewLine}");
        }

    }
}
