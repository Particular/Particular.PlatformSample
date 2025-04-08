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
        public static async Task Launch(bool showPlatformToolConsoleOutput = false, string servicePulseDefaultRoute = null, string rootFolder = null, CancellationToken cancellationToken = default)
        {
            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Console.CancelKeyPress += (sender, args) =>
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

            var launcher = new AppLauncher(showPlatformToolConsoleOutput);
            await using var _ = launcher.ConfigureAwait(false);

            Console.WriteLine("Launching RavenDB");
            var serverUri = await launcher.RavenDB(ravenLogs, ravenDB, cancellationToken).ConfigureAwait(false);

            Console.WriteLine("Launching ServiceControl");
            launcher.ServiceControl(controlPort, maintenancePort, controlLogs, transportPath, auditPort, serverUri);

            Console.WriteLine("Launching ServiceControl Audit");
            launcher.ServiceControlAudit(auditPort, auditMaintenancePort, auditLogs, transportPath, serverUri);

            Console.WriteLine("Launching ServiceControl Monitoring");
            launcher.Monitoring(monitoringPort, monitoringLogs, transportPath);

            Console.WriteLine("Launching ServicePulse");
            launcher.ServicePulse(pulsePort, controlPort, monitoringPort, servicePulseDefaultRoute);

            Console.Write("Waiting for ServiceControl to be available");
            await Network.WaitForHttpOk($"http://localhost:{controlPort}/api", cancellationToken: tokenSource.Token)
                .ConfigureAwait(false);

            if (!tokenSource.IsCancellationRequested)
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

                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, tokenSource.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (tokenSource.Token.IsCancellationRequested)
                {
                    // ignore
                }
            }

            Console.WriteLine();
            Console.WriteLine("Waiting for external processes to shut down...");
        }
    }
}
