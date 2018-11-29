namespace Particular
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// stub
    /// </summary>
    public static class PlatformLauncher
    {
        const int PortStartSearch = 33533;

        /// <summary>
        /// stub
        /// </summary>
        public static void Launch(bool showPlatformToolConsoleOutput = false) => Launch(Console.Out, Console.In, showPlatformToolConsoleOutput);

        /// <summary>
        ///
        /// </summary>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <param name="showPlatformToolConsoleOutput"></param>
        public static void Launch(TextWriter output, TextReader input, bool showPlatformToolConsoleOutput = false)
        {
            var wait = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                wait.Set();
            };

            LaunchInternal(output, input, showPlatformToolConsoleOutput, () =>
            {
                output.WriteLine("Press Ctrl+C stop Particular Service Platform tools.");
                wait.WaitOne();

                output.WriteLine();
                output.WriteLine("Waiting for external processes to shut down...");
            });

        }

        internal static void LaunchInternal(TextWriter output, TextReader input, bool showPlatformToolConsoleOutput, Action interactive)
        {
            var ports = Network.FindAvailablePorts(PortStartSearch, 4);

            var controlPort = ports[0];
            var maintenancePort = ports[1];
            var monitoringPort = ports[2];
            var pulsePort = ports[3];

            output.WriteLine($"Found free port '{controlPort}' for ServiceControl");
            output.WriteLine($"Found free port '{maintenancePort}' for ServiceControl Maintenance");
            output.WriteLine($"Found free port '{monitoringPort}' for ServiceControl Monitoring");
            output.WriteLine($"Found free port '{pulsePort}' for ServicePulse");

            var finder = new Finder();

            output.WriteLine("Solution Folder: " + finder.SolutionRoot);

            output.WriteLine("Creating log folders");
            var monitoringLogs = finder.GetDirectory(@".\.logs\monitoring");
            var controlLogs = finder.GetDirectory(@".\.logs\servicecontrol");
            var controlDB = finder.GetDirectory(@".\.db");

            output.WriteLine("Creating transport folder");
            var transportPath = finder.GetDirectory(@".\.learningtransport");

            using (var launcher = new AppLauncher(showPlatformToolConsoleOutput))
            {
                output.WriteLine("Launching ServiceControl");
                launcher.ServiceControl(controlPort, maintenancePort, controlLogs, controlDB, transportPath);

                output.WriteLine("Launching ServiceControl Monitoring");
                // Monitoring appends `.learningtransport` to the transport path on its own
                launcher.Monitoring(monitoringPort, monitoringLogs, finder.SolutionRoot);

                output.WriteLine("Launching ServicePulse");
                launcher.ServicePulse(pulsePort, controlPort, monitoringPort);

                output.WriteLine("Waiting for ServiceControl to be available...");
                Network.WaitForHttpOk($"http://localhost:{controlPort}/api", httpVerb: "GET");

                output.WriteLine("Launching ServicePulse in a browser window...");
                var servicePulseUrl = $"http://localhost:{pulsePort}";
                Process.Start(servicePulseUrl);

                output.WriteLine();
                interactive();
            }
        }
    }
}