namespace Particular
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
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
        /// <param name="servicePulseDefaultRoute">By default, the ServicePulse dashboard (/dashboard) is displayed. Set the default route to any valid ServicePulse route such as (/monitored_endpoints).</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task Launch(bool showPlatformToolConsoleOutput = false, string servicePulseDefaultRoute = null, CancellationToken cancellationToken = default)
        {
            using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine(
                    "The Particular Service Platform can currently only be run on the Windows platform.");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            var wait = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                wait.Set();
                tokenSource.Cancel();
            };

            var ports = Network.FindAvailablePorts(PortStartSearch, 6);

            var controlPort = ports[0];
            var auditPort = ports[1];
            var maintenancePort = ports[2];
            var auditMaintenancePort = ports[3];
            var monitoringPort = ports[4];
            var pulsePort = ports[5];

            Console.WriteLine($"Found free port '{controlPort}' for ServiceControl");
            Console.WriteLine($"Found free port '{auditPort}' for ServiceControl Audit");
            Console.WriteLine($"Found free port '{maintenancePort}' for ServiceControl Maintenance");
            Console.WriteLine($"Found free port '{auditMaintenancePort}' for ServiceControl Audit Maintenance");
            Console.WriteLine($"Found free port '{monitoringPort}' for ServiceControl Monitoring");
            Console.WriteLine($"Found free port '{pulsePort}' for ServicePulse");

            var finder = new Finder();

            Console.WriteLine("Solution Folder: " + finder.SolutionRoot);

            Console.WriteLine("Creating log folders");
            var monitoringLogs = finder.GetDirectory(@".\.logs\monitoring");
            var controlLogs = finder.GetDirectory(@".\.logs\servicecontrol");
            var controlDB = finder.GetDirectory(@".\.db");
            var auditLogs = finder.GetDirectory(@".\.logs\servicecontrol-audit");
            var auditDB = finder.GetDirectory(@".\.audit-db");

            Console.WriteLine("Creating transport folder");
            var transportPath = finder.GetDirectory(@".\.learningtransport");

            using var launcher = new AppLauncher(showPlatformToolConsoleOutput);

            Console.WriteLine("Launching ServiceControl");
            launcher.ServiceControl(controlPort, maintenancePort, controlLogs, controlDB, transportPath,
                auditPort);

            Console.WriteLine("Launching ServiceControl Audit");
            launcher.ServiceControlAudit(auditPort, auditMaintenancePort, auditLogs, auditDB, transportPath);

            Console.WriteLine("Launching ServiceControl Monitoring");
            launcher.Monitoring(monitoringPort, monitoringLogs, transportPath);

            Console.WriteLine("Launching ServicePulse");
            launcher.ServicePulse(pulsePort, controlPort, monitoringPort, servicePulseDefaultRoute);

            Console.Write("Waiting for ServiceControl to be available");
            await Network.WaitForHttpOk($"http://localhost:{controlPort}/api",
                    cancellationToken: tokenSource.Token)
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
                wait.WaitOne();
            }

            Console.WriteLine();
            Console.WriteLine("Waiting for external processes to shut down...");
        }
    }
}
