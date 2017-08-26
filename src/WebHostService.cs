using DasMulli.Win32.ServiceUtils;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Xakep.Hosting.WindowsServices
{
    public class WebHostService : IWin32Service
    {
        private Process HostProcess = null;
        private Settings Service = null;
        private List<string> logs;

        public WebHostService(Settings service)
        {
            logs = new List<string>();
            Service = service;
            ServiceName = service.ServiceName;
        }

        public string ServiceName { get; set; }

        public void Start(string[] startupArguments, ServiceStoppedCallback serviceStoppedCallback)
        {
            try
            {
                var binaryPath = Process.GetCurrentProcess().MainModule.FileName;
                var args = binaryPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase)
                    ? $"\"{Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, PlatformServices.Default.Application.ApplicationName + ".dll")}\" {string.Join(" ", Service.Args)}"
                    : $"{string.Join(" ", Service.Args)}";

                HostProcess = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = binaryPath;
                startInfo.Arguments = args;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.WorkingDirectory = PlatformServices.Default.Application.ApplicationBasePath;
                HostProcess.StartInfo = startInfo;
                HostProcess.EnableRaisingEvents = true;
                HostProcess.Exited += (s, _e) => { Stop(); serviceStoppedCallback(); };
                
                if (Service.WriteLog)
                {
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    HostProcess.OutputDataReceived += (s, _e) => WriteLog(_e.Data);
                    HostProcess.ErrorDataReceived += (s, _e) => WriteLog(_e.Data);
                }

                HostProcess.Start();

                if (Service.WriteLog)
                {
                    HostProcess.BeginOutputReadLine();
                    HostProcess.BeginErrorReadLine();
                }

                if (Service.WriteLog)
                    WriteLog($"{ServiceName} Started");
            }
            catch (Exception ex)
            {
                if (Service.WriteLog)
                    WriteLog($"{ServiceName} {ex.Message}");
                Stop();
                serviceStoppedCallback();
            }
        }

        public void Stop()
        {
            try
            {
                //https://github.com/dotnet/corefx/issues/1838
                try
                {
                    AttachConsole(HostProcess.Id);
                }
                catch
                {

                }
                Thread.SpinWait(10000);

                HostProcess.Kill();

                HostProcess.Dispose();

                if (Service.WriteLog)
                    WriteLog($"{ServiceName} Stopped");
                Console.WriteLine($"{ServiceName} Stopped");
            }
            catch (Exception ex)
            {
                if (Service.WriteLog)
                    WriteLog($"{ServiceName} {ex.Message}");
            }
            WriteFile(true);
        }

        private void WriteLog(string log)
        {
            lock (logs)
            {
                logs.Add($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff")} {log}" );
                WriteFile();
            }
        }
        
        private void WriteFile(bool Push=false)
        {
            lock (logs)
            {
                if (logs.Count > 500 || Push)
                {
                    File.AppendAllText(Service.LogPath, string.Join(Environment.NewLine, logs));
                    logs.Clear();
                }
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
    }
}
