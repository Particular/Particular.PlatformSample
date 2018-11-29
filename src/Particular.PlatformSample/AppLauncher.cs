namespace Particular
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;

    class AppLauncher : IDisposable
    {
        const string platformPath = @".\platform";
        ProcessCloser control;
        ProcessCloser monitoring;
        ProcessCloser pulse;
        bool hideConsoleOutput;

        public AppLauncher(bool showPlatformToolConsoleOutput)
        {
            this.hideConsoleOutput = !showPlatformToolConsoleOutput;
        }

        public void ServiceControl(int port, int maintenancePort, string logPath, string dbPath, string transportPath)
        {
            var config = GetResource("Particular.configs.ServiceControl.exe.config");

            config = config.Replace("{ServiceControlPort}", port.ToString());
            config = config.Replace("{MaintenancePort}", maintenancePort.ToString());
            config = config.Replace("{LogPath}", logPath);
            config = config.Replace("{DbPath}", dbPath);
            config = config.Replace("{TransportPath}", transportPath);

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"platform\servicecontrol\servicecontrol-instance\ServiceControl.exe.config");

            File.WriteAllText(configPath, config, Encoding.UTF8);

            var proc = StartProcess(@"servicecontrol\servicecontrol-instance\ServiceControl.exe");

            control = new ProcessCloser(proc, ProcessCloser.CtrlC);
        }

        public void Monitoring(int port, string logPath, string transportPath)
        {
            var config = GetResource("Particular.configs.ServiceControl.Monitoring.exe.config");

            config = config.Replace("{MonitoringPort}", port.ToString());
            config = config.Replace("{LogPath}", logPath);
            config = config.Replace("{TransportPath}", transportPath);

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe.config");

            File.WriteAllText(configPath, config, Encoding.UTF8);

            var proc = StartProcess(@"servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe");
            monitoring = new ProcessCloser(proc, ProcessCloser.CtrlC);
        }

        public void ServicePulse(int port, int serviceControlPort, int monitoringPort)
        {
            var config = GetResource("Particular.configs.app.constants.js");

            config = config.Replace("{ServiceControlPort}", serviceControlPort.ToString());
            config = config.Replace("{MonitoringPort}", monitoringPort.ToString());

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"platform\servicepulse\js\app.constants.js");

            File.WriteAllText(configPath, config, Encoding.UTF8);

            var webroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"platform\servicepulse");
            var sp = new ServicePulse(port, webroot);

            sp.Run();

            pulse = new ProcessCloser(null, _ => sp.Stop());

            var url = $"http://localhost:{port}";
            Process.Start(url);
        }

        Process StartProcess(string relativeExePath, string arguments = null)
        {
            var fullExePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, platformPath, relativeExePath));
            var workingDirectory = Path.GetDirectoryName(fullExePath);

            var startInfo = new ProcessStartInfo(fullExePath, arguments);
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.UseShellExecute = false;

            if (hideConsoleOutput)
            {
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
            }


            return Process.Start(startInfo);
        }

        static string GetResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        void CloseAll()
        {
            pulse?.Close();
            monitoring?.Close();
            control?.Close();
        }

        public void Dispose()
        {
            CloseAll();
            GC.SuppressFinalize(this);
        }

        ~AppLauncher()
        {
            CloseAll();
        }

        class ProcessCloser
        {
            Process process;
            Action<Process> closeAction;

            public ProcessCloser(Process process, Action<Process> closeAction)
            {
                this.process = process;
                this.closeAction = closeAction;
            }
            
            public static Action<Process> CtrlC => process =>
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.WaitForExit();
                    }
                    catch (InvalidOperationException iox)
                    {
                        Console.WriteLine(iox);
                    }
                }
            };

            public void Close()
            {
                if (process != null && process.HasExited)
                {
                    return;
                }

                try
                {
                    closeAction(process);
                }
                catch (InvalidOperationException x)
                {
                    Console.WriteLine(x.Message);
                    Console.WriteLine(x.StackTrace);
                }
                finally
                {
                    process?.Dispose();
                }
            }
        }
    }


}
