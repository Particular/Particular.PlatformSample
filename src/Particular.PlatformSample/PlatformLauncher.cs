namespace Particular
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// Launches Particular Service Platform tools (ServiceControl, ServiceControl Monitoring, and ServicePulse) in a single process
    /// using the Learning Transport in order to demonstrate platform usage in a sample or test project. Not to be used outside of a
    /// test/demo context. In real life, each tool should be installed as a Windows service, and used with a production-ready
    /// message transport.
    /// </summary>
    public static class PlatformLauncher
    {
        const int PortStartSearch = 49_200;

        static CancellationTokenSource tokenSource = new CancellationTokenSource();

        /// <summary>
        /// Launches Particular Service Platform tools (ServiceControl, ServiceControl Monitoring, and ServicePulse) in a single process
        /// using the Learning Transport in order to demonstrate platform usage in a sample or test project. Not to be used outside of a
        /// test/demo context. In real life, each tool should be installed as a Windows service, and used with a production-ready
        /// message transport.
        /// </summary>
        /// <param name="showPlatformToolConsoleOutput">By default the output of each application is suppressed. Set to true to show tool output in the console window.</param>
        public static void Launch(bool showPlatformToolConsoleOutput = false)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("The Particular Service Platform can currently only be run on the Windows platform.");
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

            var ports = Network.FindAvailablePorts(PortStartSearch, 4);

            var controlPort = ports[0];
            var maintenancePort = ports[1];
            var monitoringPort = ports[2];
            var pulsePort = ports[3];

            Console.WriteLine($"Found free port '{controlPort}' for ServiceControl");
            Console.WriteLine($"Found free port '{maintenancePort}' for ServiceControl Maintenance");
            Console.WriteLine($"Found free port '{monitoringPort}' for ServiceControl Monitoring");
            Console.WriteLine($"Found free port '{pulsePort}' for ServicePulse");

            var finder = new Finder();

            Console.WriteLine("Solution Folder: " + finder.SolutionRoot);

            Console.WriteLine("Creating log folders");
            var monitoringLogs = finder.GetDirectory(@".\.logs\monitoring");
            var controlLogs = finder.GetDirectory(@".\.logs\servicecontrol");
            var controlDB = finder.GetDirectory(@".\.db");

            Console.WriteLine("Creating transport folder");
            var transportPath = finder.GetDirectory(@".\.learningtransport");

            using (var launcher = new AppLauncher(showPlatformToolConsoleOutput))
            {
                Console.WriteLine("Launching ServiceControl");
                launcher.ServiceControl(controlPort, maintenancePort, controlLogs, controlDB, transportPath);

                Console.WriteLine("Launching ServiceControl Monitoring");
                // Monitoring appends `.learningtransport` to the transport path on its own
                launcher.Monitoring(monitoringPort, monitoringLogs, finder.SolutionRoot);

                Console.WriteLine("Launching ServicePulse");
                launcher.ServicePulse(pulsePort, controlPort, monitoringPort);

                Console.WriteLine("Waiting for ServiceControl to be available...");
                Network.WaitForHttpOk($"http://localhost:{controlPort}/api", httpVerb: "GET", cancellationToken: tokenSource.Token);

                if (!tokenSource.IsCancellationRequested)
                {
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
}