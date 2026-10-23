namespace Particular.PlatformSample.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class ChildProcessTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task ReportsMissingRuntimeEvenWhenConsoleOutputIsHidden(bool showConsoleOutput)
        {
            var runtimeConfig = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.runtimeconfig.json");
            try
            {
                await File.WriteAllTextAsync(runtimeConfig, """
                    {
                      "runtimeOptions": {
                        "tfm": "net10.0",
                        "rollForward": "Disable",
                        "framework": {
                          "name": "Microsoft.NETCore.App",
                          "version": "999.0.0"
                        }
                      }
                    }
                    """);

                var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet");
                startInfo.ArgumentList.Add("exec");
                startInfo.ArgumentList.Add("--runtimeconfig");
                startInfo.ArgumentList.Add(runtimeConfig);
                startInfo.ArgumentList.Add(typeof(ChildProcessTests).Assembly.Location);
                startInfo.Environment.Remove("DOTNET_ROLL_FORWARD");

                await using var child = new ChildProcess("ServiceControl", startInfo, showConsoleOutput);
                var failure = await child.Exited.WaitAsync(TimeSpan.FromSeconds(15));

                Assert.Multiple(() =>
                {
                    Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                    Assert.That(failure.Message, Does.Contain("ServiceControl").And.Contain("exit code"));
                    Assert.That(failure.Message, Does.Contain("Microsoft.NETCore.App").And.Contain("999.0.0"));
                });
            }
            finally
            {
                File.Delete(runtimeConfig);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ReportsUnexpectedSuccessfulExitAndCapturesStandardOutput(bool showConsoleOutput)
        {
            var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet");
            startInfo.ArgumentList.Add("--list-runtimes");

            await using var child = new ChildProcess("ServiceControl.Monitoring", startInfo, showConsoleOutput);
            var failure = await child.Exited.WaitAsync(TimeSpan.FromSeconds(15));

            Assert.Multiple(() =>
            {
                Assert.That(failure.Message, Does.Contain("ServiceControl.Monitoring").And.Contain("exit code 0"));
                Assert.That(failure.Message, Does.Contain("Microsoft.NETCore.App"));
            });
        }
    }
}
