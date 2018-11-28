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

        public void Monitoring(int port)
        {
            var proc = StartProcess(@"servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe");
            monitoring = new ProcessCloser(proc, ProcessCloser.CtrlC);
        }

        public void ServicePulse(int port)
        {
            var proc = StartProcess(@"servicepulse\ServicePulse.Host.exe", $"--url=\"http://localhost:{port}\"");
            pulse = new ProcessCloser(proc, ProcessCloser.CtrlC);
        }

        static Process StartProcess(string relativeExePath, string arguments = null)
        {
            var fullExePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, platformPath, relativeExePath));
            var workingDirectory = Path.GetDirectoryName(fullExePath);

            var startInfo = new ProcessStartInfo(fullExePath, arguments);
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.Verb = "runAs";
            //startInfo.WindowStyle = ProcessWindowStyle.Minimized;

            var process = Process.Start(startInfo);
            return process;
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

            public static Action<Process> CloseMainWindow => process => process.CloseMainWindow();
            public static Action<Process> CtrlC => process =>
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException) { }
                }
            };

            public void Close()
            {
                if (process != null && !process.HasExited)
                {
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
                        process.Dispose();
                    }
                }
            }
        }
    }


}
