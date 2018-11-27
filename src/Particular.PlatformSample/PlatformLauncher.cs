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
            var ports = Network.FindAvailablePorts(PortStartSearch, 4);

            var controlPort = ports[0];
            var maintenancePort = ports[1];
            var monitoringPort = ports[2];
            var pulsePort = ports[3];

            output.WriteLine($"Found free port '{controlPort}' for ServiceControl");
            output.WriteLine($"Found free port '{maintenancePort}' for ServiceControl Maintenance");
            output.WriteLine($"Found free port '{monitoringPort}' for ServiceControl Monitoring");
            output.WriteLine($"Found free port '{pulsePort}' for ServicePulse");

            var solutionFolder = Finder.FindSolutionRoot();
            output.WriteLine("Solution Folder: " + solutionFolder);

            output.WriteLine("Creating log folders");
            Directory.CreateDirectory(Path.Combine(solutionFolder, @".\.logs\monitoring"));
            Directory.CreateDirectory(Path.Combine(solutionFolder, @".\.logs\servicecontrol"));

            output.WriteLine("Creating transport folder");
            Directory.CreateDirectory(Path.Combine(solutionFolder, @".\.learningtransport"));

            using (var launcher = new AppLauncher())
            {
                output.WriteLine("Launching ServiceControl");
                launcher.ServiceControl(controlPort, maintenancePort);

                output.WriteLine("Waiting for ServiceControl to be available...");
                //Network.WaitForHttpOk($"http://localhost:{controlPort}");

                output.WriteLine("Launching ServiceControl Monitoring");
                launcher.Monitoring(monitoringPort);

                output.WriteLine("Launching ServicePulse");
                launcher.ServicePulse(pulsePort);

                output.WriteLine("Press Enter to stop Particular Service Platform tools.");
                input.ReadLine();
            }
        }   
    }
}