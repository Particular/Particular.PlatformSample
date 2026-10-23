namespace Particular
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Launches Particular Service Platform tools (ServiceControl, ServiceControl Monitoring, and ServicePulse) in a single process
    /// using the Learning Transport in order to demonstrate platform usage in a sample or test project. Not to be used outside of a
    /// test/demo context. In real life, each tool should be installed as a Windows service, and used with a production-ready
    /// message transport.
    /// </summary>
    public static class PlatformLauncher
    {
        const int PortStartSearch = 49_200;

        /// <summary>
        /// Launches Particular Service Platform tools (ServiceControl, ServiceControl Monitoring, and ServicePulse) in a single process
        /// using the Learning Transport in order to demonstrate platform usage in a sample or test project. Not to be used outside of a
        /// test/demo context. In real life, each tool should be installed as a Windows service, and used with a production-ready
        /// message transport.
        /// </summary>
        /// <param name="showPlatformToolConsoleOutput">By default, the output of each application is suppressed. Set to true to show tool output in the console window.</param>
        /// <param name="servicePulseDefaultRoute">By default, the ServicePulse dashboard (/dashboard) is displayed. Set the default route to any valid ServicePulse route such as (/monitoring).</param>
        /// <param name="rootFolder">Overrides the root folder used to store all the platform data. By default the root folder is determined by searching for a solution file.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Unexpected tool exits are reported to standard error. Use <see cref="LaunchWithResult"/> to inspect the outcome.
        /// </remarks>
        public static async Task Launch(bool showPlatformToolConsoleOutput = false, string servicePulseDefaultRoute = null, string rootFolder = null, CancellationToken cancellationToken = default)
        {
            await LaunchWithResult(showPlatformToolConsoleOutput, servicePulseDefaultRoute, rootFolder, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs the platform tools until shutdown is requested or a tool exits, and returns the outcome after cleanup.
        /// Unexpected tool exits are also reported to standard error.
        /// </summary>
        /// <param name="showPlatformToolConsoleOutput">Whether to show tool console output while the tools are running.</param>
        /// <param name="servicePulseDefaultRoute">The ServicePulse route to open. Defaults to the dashboard.</param>
        /// <param name="rootFolder">Overrides the root folder used for platform data.</param>
        /// <param name="cancellationToken">Requests shutdown.</param>
        /// <returns>A result identifying an unexpected tool exit, or a result with no unexpected exit when shutdown was requested.</returns>
        public static async Task<LaunchResult> LaunchWithResult(bool showPlatformToolConsoleOutput = false, string servicePulseDefaultRoute = null, string rootFolder = null, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new LaunchResult();
            }

            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            ConsoleCancelEventHandler cancelHandler = (sender, args) =>
            {
                args.Cancel = true;
                try
                {
                    tokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
            };

            var ports = Network.FindAvailablePorts(PortStartSearch, 6);

            var controlPort = ports[0];
            var auditPort = ports[1];
            var maintenancePort = ports[2];
            var auditMaintenancePort = ports[3];
            var monitoringPort = ports[4];
            var pulsePort = ports[5];

            TestPortsInternal.ServicePulse = pulsePort;

            Console.WriteLine($"Found free port '{controlPort}' for ServiceControl");
            Console.WriteLine($"Found free port '{auditPort}' for ServiceControl Audit");
            Console.WriteLine($"Found free port '{maintenancePort}' for ServiceControl Maintenance");
            Console.WriteLine($"Found free port '{auditMaintenancePort}' for ServiceControl Audit Maintenance");
            Console.WriteLine($"Found free port '{monitoringPort}' for ServiceControl Monitoring");
            Console.WriteLine($"Found free port '{pulsePort}' for ServicePulse");

            var finder = rootFolder != null
                ? new Finder(rootFolder)
                : new Finder();

            Console.WriteLine("Root Folder: " + finder.Root);

            Console.WriteLine("Creating log folders");
            var ravenLogs = finder.GetDirectory(Path.Combine(".logs", "raven"));
            var monitoringLogs = finder.GetDirectory(Path.Combine(".logs", "monitoring"));
            var controlLogs = finder.GetDirectory(Path.Combine(".logs", "servicecontrol"));
            var auditLogs = finder.GetDirectory(Path.Combine(".logs", "servicecontrol-audit"));

            var ravenDB = finder.GetDirectory(".db");

            Console.WriteLine("Creating transport folder");
            var transportPath = finder.GetDirectory(".learningtransport");

            var launcher = new AppLauncher(showPlatformToolConsoleOutput, shutdownCancellationToken: cancellationToken);
            await using var _ = launcher.ConfigureAwait(false);

            Task startup = Task.CompletedTask;
            Console.CancelKeyPress += cancelHandler;
            try
            {
                Console.WriteLine("Launching RavenDB");
                Uri serverUri;
                try
                {
                    serverUri = await launcher.RavenDB(ravenLogs, ravenDB, tokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (tokenSource.Token.IsCancellationRequested)
                {
                    return new LaunchResult();
                }

                Console.WriteLine("Launching ServiceControl");
                launcher.ServiceControl(controlPort, maintenancePort, controlLogs, transportPath, auditPort, serverUri);

                Console.WriteLine("Launching ServiceControl Audit");
                launcher.ServiceControlAudit(auditPort, auditMaintenancePort, auditLogs, transportPath, serverUri);

                Console.WriteLine("Launching ServiceControl Monitoring");
                launcher.Monitoring(monitoringPort, monitoringLogs, transportPath);

                Console.WriteLine("Launching ServicePulse");
                launcher.ServicePulse(pulsePort, controlPort, monitoringPort, servicePulseDefaultRoute);

                Console.Write("Waiting for ServiceControl to be available");
                var unexpectedExitTask = launcher.WaitForFirstExit(CancellationToken.None);
                startup = Network.WaitForHttpOk($"http://localhost:{controlPort}/api", cancellationToken: tokenSource.Token);
                var unexpectedExit = await WaitForTaskOrUnexpectedExit(startup, unexpectedExitTask, tokenSource.Token).ConfigureAwait(false);

                if (unexpectedExit == null && !tokenSource.IsCancellationRequested)
                {
                    var serviceControlApiUrl = $"http://localhost:{controlPort}/api";
                    Console.WriteLine();
                    Console.WriteLine($"ServiceControl API can now be accessed via: {serviceControlApiUrl}");

                    var servicePulseUrl = $"http://localhost:{pulsePort}";
                    Console.WriteLine();
                    Console.WriteLine($"ServicePulse can now be accessed via: {servicePulseUrl}");
                    Console.WriteLine("Attempting to launch ServicePulse in a browser window...");
                    Process.Start(new ProcessStartInfo(servicePulseUrl) { UseShellExecute = true });

                    Console.WriteLine();
                    Console.WriteLine("Press Ctrl+C stop Particular Service Platform tools.");

                    unexpectedExit = await WaitForTaskOrUnexpectedExit(Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None), unexpectedExitTask, tokenSource.Token)
                        .ConfigureAwait(false);
                }

                if (unexpectedExit != null)
                {
                    await Console.Error.WriteLineAsync().ConfigureAwait(false);
                    await Console.Error.WriteLineAsync($"Platform tool '{unexpectedExit.ProcessName}' exited unexpectedly with exit code {unexpectedExit.ExitCode}.{Environment.NewLine}Standard output:{Environment.NewLine}{unexpectedExit.StandardOutput}{Environment.NewLine}Standard error:{Environment.NewLine}{unexpectedExit.StandardError}")
                        .ConfigureAwait(false);
                }

                Console.WriteLine();
                Console.WriteLine("Waiting for external processes to shut down...");
                return new LaunchResult(unexpectedExit);
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
                await tokenSource.CancelAsync().ConfigureAwait(false);
                try
                {
                    await startup.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (tokenSource.Token.IsCancellationRequested)
                {
                    // The readiness request is cancelled when a child exits or the launcher stops.
                }
            }
        }

        internal static async Task<ProcessExit> WaitForTaskOrUnexpectedExit(Task operationTask, Task<ProcessExit> unexpectedExitTask, CancellationToken cancellationToken = default)
        {
            var shutdownRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = cancellationToken.Register(() => shutdownRequested.TrySetResult());
            var completedTask = await Task.WhenAny(operationTask, unexpectedExitTask, shutdownRequested.Task).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (completedTask == unexpectedExitTask)
            {
                return await unexpectedExitTask.ConfigureAwait(false);
            }

            await operationTask.ConfigureAwait(false);
            return null;
        }
    }
}
