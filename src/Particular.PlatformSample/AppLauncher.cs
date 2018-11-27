namespace Particular
{
    using System;
    using System.Diagnostics;
    using System.IO;

    class AppLauncher : IDisposable
    {
        const string platformPath = @".\platform";
        Process control;
        Process monitoring;
        Process pulse;

        public void ServiceControl(int port, int maintenancePort)
        {
            control = StartProcess(@"servicecontrol\servicecontrol-instance\bin\ServiceControl.exe");
        }

        public void Monitoring(int port)
        {
            monitoring = StartProcess(@"servicecontrol\monitoring-instance\ServiceControl.Monitoring.exe");
        }

        public void ServicePulse(int port)
        {
            pulse = StartProcess(@"servicepulse\ServicePulse.Host.exe", $"--url=\"http://localhost:{port}\"");
        }

        static Process StartProcess(string relativeExePath, string arguments = null)
        {
            var fullExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, platformPath, relativeExePath);
            var workingDirectory = Path.GetDirectoryName(fullExePath);

            var startInfo = new ProcessStartInfo(fullExePath, arguments);
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.Verb = "runAs";
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;

            var process = Process.Start(startInfo);
            return process;
        }

        void CloseAll()
        {
            pulse?.CloseMainWindow();
            pulse?.Dispose();

            monitoring?.CloseMainWindow();
            monitoring?.Dispose();

            control?.CloseMainWindow();
            control?.Dispose();
        }

        public void Dispose()
        {
            CloseAll();
            GC.SuppressFinalize(this);
        }

        ~AppLauncher()
        {
            CloseAll();
        }
    }
}
