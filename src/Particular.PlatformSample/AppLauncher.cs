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

    sealed class AppLauncher : IAsyncDisposable
    {
        readonly string platformPath;
        readonly bool hideConsoleOutput;
        readonly Stack<Func<ValueTask>> cleanupActions;

        public AppLauncher(bool showPlatformToolConsoleOutput)
        {
            platformPath = Path.Combine(AppContext.BaseDirectory, "platform");
            hideConsoleOutput = !showPlatformToolConsoleOutput;
            cleanupActions = new Stack<Func<ValueTask>>();
        }

        public Task<Uri> RavenDB(string logsPath, string dataDirectory, CancellationToken cancellationToken = default)
        {
            var licenseFilePath = Path.Combine(platformPath, "servicecontrol", "servicecontrol-instance", "Persisters", "RavenDB", "RavenLicense.json");

            var options = new ServerOptions
            {
                LogsPath = logsPath,
                DataDirectory = dataDirectory,
                CommandLineArgs = [$"--License.Path=\"{licenseFilePath}\""]
            };

            EmbeddedServer.Instance.StartServer(options);
            cleanupActions.Push(() => StopRavenDB(cancellationToken));

            return EmbeddedServer.Instance.GetServerUriAsync(cancellationToken);
        }

        static async ValueTask StopRavenDB(CancellationToken cancellationToken)
        {
            var disposeTask = Task.Run(() => EmbeddedServer.Instance.Dispose(), CancellationToken.None);

            // Runs "infinite" but will eventually get cancelled
            var waitForCancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);

            var delayIsCancelled = waitForCancellationTask == await Task.WhenAny(disposeTask, waitForCancellationTask)
                .ConfigureAwait(false);

            if (delayIsCancelled)
            {
                int processId = 0;
                try
                {
                    // We always want to try and kill the process, even when already cancelled
                    processId = await EmbeddedServer.Instance.GetServerProcessIdAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    await Console.Error.WriteLineAsync($"Killing RavenDB server PID {processId} because host cancelled")
                        .ConfigureAwait(false);
                    using var ravenChildProcess = Process.GetProcessById(processId);
                    ravenChildProcess.Kill(entireProcessTree: true);
                    // Kill only signals
                    await Console.Error.WriteLineAsync($"Waiting for RavenDB server PID {processId} to exit... ")
                        .ConfigureAwait(false);
                    // When WaitForExitAsync returns, the process could still exist but in a frozen state to flush
                    // memory mapped pages to storage.
                    await ravenChildProcess.WaitForExitAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await Console.Error.WriteLineAsync($"Failed to kill RavenDB server PID {processId} shutdown\n{e.Message}")
                        .ConfigureAwait(false);
                }
            }
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

            var configPath = Path.Combine(platformPath, "servicecontrol", "servicecontrol-instance", "ServiceControl.exe.config");

            File.WriteAllText(configPath, config, Encoding.UTF8);
            cleanupActions.Push(() =>
            {
                File.Delete(configPath);
                return ValueTask.CompletedTask;
            });

            StartProcess(Path.Combine(platformPath, "servicecontrol", "servicecontrol-instance", "ServiceControl.dll"));
        }

        public void ServiceControlAudit(int port, int maintenancePort, string logPath, string transportPath, Uri connectionString)
        {
            var config = GetResource("Particular.configs.ServiceControl.Audit.exe.config");

            config = config.Replace("{Port}", port.ToString());
            config = config.Replace("{MaintenancePort}", maintenancePort.ToString());
            config = config.Replace("{LogPath}", logPath);
            config = config.Replace("{ConnectionString}", connectionString.ToString());
            config = config.Replace("{TransportPath}", transportPath);

            var configPath = Path.Combine(platformPath, "servicecontrol", "servicecontrol-audit-instance", "ServiceControl.Audit.exe.config");

            File.WriteAllText(configPath, config, Encoding.UTF8);
            cleanupActions.Push(() =>
            {
                File.Delete(configPath);
                return ValueTask.CompletedTask;
            });

            StartProcess(Path.Combine(platformPath, "servicecontrol", "servicecontrol-audit-instance", "ServiceControl.Audit.dll"));
        }

        public void Monitoring(int port, string logPath, string transportPath)
        {
            var config = GetResource("Particular.configs.ServiceControl.Monitoring.exe.config");

            config = config.Replace("{MonitoringPort}", port.ToString());
            config = config.Replace("{LogPath}", logPath);
            config = config.Replace("{TransportPath}", transportPath);

            var configPath = Path.Combine(platformPath, "servicecontrol", "monitoring-instance", "ServiceControl.Monitoring.exe.config");

            File.WriteAllText(configPath, config, Encoding.UTF8);
            cleanupActions.Push(() =>
            {
                File.Delete(configPath);
                return ValueTask.CompletedTask;
            });

            StartProcess(Path.Combine(platformPath, "servicecontrol", "monitoring-instance", "ServiceControl.Monitoring.dll"));
        }

        public void ServicePulse(int port, int serviceControlPort, int monitoringPort, string defaultRoute)
        {
            var environmentVariables = new Dictionary<string, string>
            {
                { "ASPNETCORE_HTTP_PORTS", port.ToString() },
                { "SERVICECONTROL_URL", $"http://localhost:{serviceControlPort}" },
                { "MONITORING_URL", $"http://localhost:{monitoringPort}" },
                { "DEFAULT_ROUTE", defaultRoute }
            };

            StartProcess(Path.Combine(platformPath, "servicepulse", "ServicePulse.dll"), environmentVariables);
        }

        void StartProcess(string assemblyPath, Dictionary<string, string> environmentVariables = null)
        {
            var workingDirectory = Path.GetDirectoryName(assemblyPath);

            var startInfo = new ProcessStartInfo("dotnet", assemblyPath)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false
            };

            if (environmentVariables is not null)
            {
                foreach (var item in environmentVariables)
                {
                    startInfo.EnvironmentVariables.Add(item.Key, item.Value);
                }
            }

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

        public async ValueTask DisposeAsync()
        {
            while (cleanupActions.Count > 0)
            {
                var action = cleanupActions.Pop();
                await action().ConfigureAwait(false);
            }
        }
    }
}
