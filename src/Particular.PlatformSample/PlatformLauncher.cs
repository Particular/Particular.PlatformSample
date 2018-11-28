namespace Particular
{
    using System;
    using System.IO;

    /// <summary>
    /// stub
    /// </summary>
    public static class PlatformLauncher
    {
        const int PortStartSearch = 33533;

        /// <summary>
        /// stub
        /// </summary>
        public static void Launch() => Launch(Console.Out, Console.In);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        /// <param name="input"></param>
        public static void Launch(TextWriter output, TextReader input)
        {
            LaunchInternal(output, input, () =>
            {
                output.WriteLine("Press Enter to stop Particular Service Platform tools.");
                input.ReadLine();
            });
        }

        internal static void LaunchInternal(TextWriter output, TextReader input, Action interactive)
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
            Directory.CreateDirectory(transportPath);

            using (var launcher = new AppLauncher())
            {
                output.WriteLine("Launching ServiceControl");
                launcher.ServiceControl(controlPort, maintenancePort, controlLogs, controlDB, transportPath);

                output.WriteLine("Waiting for ServiceControl to be available...");
                Network.WaitForHttpOk($"http://localhost:{controlPort}/api", httpVerb: "GET");

                output.WriteLine("Launching ServiceControl Monitoring");
                launcher.Monitoring(monitoringPort, monitoringLogs, transportPath);

                output.WriteLine("Launching ServicePulse");
                launcher.ServicePulse(pulsePort, controlPort, monitoringPort);

                interactive();
            }
        }


    }
}