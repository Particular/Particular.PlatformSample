namespace Particular
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;

    class AppLauncher : IDisposable
    {
        const string platformPath = @".\platform";
        readonly bool hideConsoleOutput;
        readonly Stack<Action> cleanupActions;
        readonly Job platformJob;

        public AppLauncher(bool showPlatformToolConsoleOutput)
        {
            platformJob = new Job("Particular.PlatformSample");
            hideConsoleOutput = !showPlatformToolConsoleOutput;
            cleanupActions = new Stack<Action>();
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
            platformJob.AddProcess(proc);
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
            platformJob.AddProcess(proc);
        }

        public void ServicePulse(int port, int serviceControlPort, int monitoringPort, string defaultRoute)
        {
            var config = GetResource("Particular.configs.app.constants.js");

            config = config.Replace("{DefaultRoute}", defaultRoute ?? "/dashboard");
            config = config.Replace("{ServiceControlPort}", serviceControlPort.ToString());
            config = config.Replace("{MonitoringPort}", monitoringPort.ToString());

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"platform\servicepulse\js\app.constants.js");

            File.WriteAllText(configPath, config, Encoding.UTF8);

            var webroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"platform\servicepulse");
            var sp = new ServicePulse(port, webroot);

            sp.Run();

            cleanupActions.Push(sp.Stop);
        }

        Process StartProcess(string relativeExePath, string arguments = null)
        {
            var fullExePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, platformPath, relativeExePath));
            var workingDirectory = Path.GetDirectoryName(fullExePath);

            var startInfo = new ProcessStartInfo(fullExePath, arguments)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false
            };

            if (hideConsoleOutput)
            {
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
            }

            var process = Process.Start(startInfo);

            // without this in certain occasions the start takes waaaaay longer!
            if (hideConsoleOutput)
            {
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
            }

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

        public void Dispose()
        {
            while (cleanupActions.Count > 0)
            {
                var action = cleanupActions.Pop();
                action();
            }

            platformJob.Dispose();
        }
    }


}
