namespace Particular
{
    using System;
    using System.IO;
    using System.Linq;

    public static class PlatformLauncher
    {
        public static void Launch() => Launch(Console.Out, Console.In);

        public static void Launch(TextWriter output, TextReader input, int serviceControlPort = 33533, int serviceControlMaintenancePort = 33534)
        {
            output.WriteLine($"Checking if port '{serviceControlPort}' is available for ServiceControl");
            var foundServiceControlPort = Network.FindAvailablePorts(serviceControlPort, 10).FirstOrDefault();
            output.WriteLine($"Found port '{foundServiceControlPort}' available for ServiceControl");

            output.WriteLine($"Checking if maintenance port '{serviceControlMaintenancePort}' is available for ServiceControl");
            var foundServiceControlMaintenancePort = Network.FindAvailablePorts(serviceControlMaintenancePort, 10).FirstOrDefault();
            output.WriteLine($"Found maintenance port '{foundServiceControlMaintenancePort}' available for ServiceControl");

            var solutionFolder = Finder.FindSolutionRoot();

            output.WriteLine("Creating log folders");
            Directory.CreateDirectory(Path.Combine(solutionFolder, @".\.logs\monitoring"));
            Directory.CreateDirectory(Path.Combine(solutionFolder, @".\.logs\servicecontrol"));

            output.WriteLine("Creating transport folder");
            Directory.CreateDirectory(Path.Combine(solutionFolder, @".\.learningtransport"));

            using (var launcher = new AppLauncher())
            {
                launcher.ServiceControl(foundServiceControlPort);

                input.ReadLine();
            }
        }   
    }
}