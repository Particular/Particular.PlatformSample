namespace Particular
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Raven.Embedded;

    class AppLauncher : IDisposable
    {
        const string platformPath = @".\platform";
        readonly bool hideConsoleOutput;
        readonly Stack<Action> cleanupActions;

        public AppLauncher(bool showPlatformToolConsoleOutput)
        {
            hideConsoleOutput = !showPlatformToolConsoleOutput;
            cleanupActions = new Stack<Action>();
        }

        public Task<Uri> RavenDB(string logsPath, string dataDirectory, CancellationToken cancellationToken = default)
        {
            var options = new ServerOptions
            {
                LogsPath = logsPath,
                DataDirectory = dataDirectory
            };

            EmbeddedServer.Instance.StartServer(options);
            cleanupActions.Push(EmbeddedServer.Instance.Dispose);

            return EmbeddedServer.Instance.GetServerUriAsync(cancellationToken);
        }

        public void ServiceControl(int port, int maintenancePort, string logPath, string transportPath, int auditPort, Uri connectionString)
        {
            var config = GetResource("Particular.configs.ServiceControl.exe.config");

            config = config.Replace("{ServiceControlPort}", port.ToString());
            config = config.Replace("{AuditPort}", auditPort.ToString());
            config = config.Replace("{MaintenancePort}", maintenancePort.ToString());
            config = config.Replace("{LogPath}", logPath);
            config = config.Replace("{ConnectionString}", connectionString.ToString());
            config = config.Replace("{TransportPath}", transportPath);

            var configPath = Path.Combine(AppContext.BaseDirectory, @"platform\servicecontrol\servicecontrol-instance\ServiceControl.exe.config");

            File.WriteAllText(configPath, config, Encoding.UTF8);

            StartProcess(@"servicecontrol\servicecontrol-instance\ServiceControl.exe");
        }

        public void ServiceControlAudit(int port, int maintenancePort, string logPath, string dbPath, string transportPath)
        {
            var config = GetResource("Particular.configs.ServiceControl.Audit.exe.config");

            config = config.Replace("{Port}", port.ToString());
            config = config.Replace("{MaintenancePort}", maintenancePort.ToString());
            config = config.Replace("{LogPath}", logPath);
            config = config.Replace("{DbPath}", dbPath);
            config = config.Replace("{TransportPath}", transportPath);

            var configPath = Path.Combine(AppContext.BaseDirectory, @"platform\servicecontrol\servicecontrol-audit-instance\ServiceControl.Audit.exe.config");

            File.WriteAllText(configPath, config, Encoding.UTF8);

            StartProcess(@"servicecontrol\servicecontrol-audit-instance\ServiceControl.Audit.exe");
        }

        public void Monitoring(int port, string logPath, string transportPath)
        {
            var config = GetResource("Particular.configs.ServiceControl.Monitoring.exe.config");

            config = config.Replace("{MonitoringPort}", port.ToString());
            config = config.Replace("{LogPath}", logPath);
            config = config.Replace("{TransportPath}", transportPath);

            var configPath = Path.Combine(AppContext.BaseDirectory, @"platform\servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe.config");

            File.WriteAllText(configPath, config, Encoding.UTF8);

            StartProcess(@"servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe");
        }

        public void ServicePulse(int port, int serviceControlPort, int monitoringPort, string defaultRoute)
        {
            var config = GetResource("Particular.configs.app.constants.js");

            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>();

            foreach (var attribute in attributes)
            {
                if (attribute.Key == "ServicePulseVersion")
                {
                    config = config.Replace("{Version}", attribute.Value);
                    break;
                }
            }

            config = config.Replace("{DefaultRoute}", defaultRoute ?? "/dashboard");
            config = config.Replace("{ServiceControlPort}", serviceControlPort.ToString());
            config = config.Replace("{MonitoringPort}", monitoringPort.ToString());

            var configPath = Path.Combine(AppContext.BaseDirectory, @"platform\servicepulse\js\app.constants.js");

            File.WriteAllText(configPath, config, Encoding.UTF8);

            var webroot = Path.Combine(AppContext.BaseDirectory, @"platform\servicepulse");
            var sp = new ServicePulse(port, webroot);

            sp.Run();

            cleanupActions.Push(sp.Stop);
        }

        void StartProcess(string relativeExePath, string arguments = null)
        {
            var fullExePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, platformPath, relativeExePath));
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
        }

        static string GetResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        public void Dispose()
        {
            while (cleanupActions.Count > 0)
            {
                var action = cleanupActions.Pop();
                action();
            }
        }
    }


}
