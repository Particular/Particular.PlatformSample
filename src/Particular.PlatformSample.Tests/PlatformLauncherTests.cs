namespace Particular.PlatformSample.Tests
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class PlatformLauncherTests
    {
        [TestCase(0)]
        [TestCase(1)]
        public async Task ReturnsUnexpectedExitWithoutFaulting(int exitCode)
        {
            var exit = new ProcessExit("ServiceControl", exitCode, "output", "error");
            var operation = new TaskCompletionSource();

            var result = await PlatformLauncher.WaitForTaskOrUnexpectedExit(operation.Task, Task.FromResult(exit), CancellationToken.None);

            Assert.That(result, Is.SameAs(exit));
        }

        [Test]
        public async Task ContinuesMonitoringAfterReadinessCompletes()
        {
            var exitSource = new TaskCompletionSource<ProcessExit>();
            var startupResult = await PlatformLauncher.WaitForTaskOrUnexpectedExit(Task.CompletedTask, exitSource.Task, CancellationToken.None);
            Assert.That(startupResult, Is.Null);

            var operation = new TaskCompletionSource();
            var running = PlatformLauncher.WaitForTaskOrUnexpectedExit(operation.Task, exitSource.Task, CancellationToken.None);
            var exit = new ProcessExit("ServiceControl.Audit", 1, "", "stopped");
            exitSource.SetResult(exit);

            Assert.That(await running, Is.SameAs(exit));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task RequestedShutdownTakesPrecedenceOverToolExit(bool toolAlreadyExited)
        {
            using var shutdown = new CancellationTokenSource();
            shutdown.Cancel();
            var operation = new TaskCompletionSource();
            var exitSource = new TaskCompletionSource<ProcessExit>();
            if (toolAlreadyExited)
            {
                exitSource.SetResult(new ProcessExit("ServiceControl", 0, "", ""));
            }

            var result = await PlatformLauncher.WaitForTaskOrUnexpectedExit(operation.Task, exitSource.Task, shutdown.Token);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task PropagatesOperationErrors()
        {
            var exitSource = new TaskCompletionSource<ProcessExit>();
            var operation = Task.FromException(new IOException("Readiness failed."));

            await Assert.ThrowsAsync<IOException>(async () =>
                await PlatformLauncher.WaitForTaskOrUnexpectedExit(operation, exitSource.Task, CancellationToken.None));
        }

        [Test]
        public async Task PropagatesMonitoringErrors()
        {
            var operation = new TaskCompletionSource();
            var exitTask = Task.FromException<ProcessExit>(new IOException("Monitoring failed."));

            await Assert.ThrowsAsync<IOException>(async () =>
                await PlatformLauncher.WaitForTaskOrUnexpectedExit(operation.Task, exitTask, CancellationToken.None));
        }

        [Test]
        public async Task CancellationBeforeLaunchReturnsRequestedShutdown()
        {
            using var shutdown = new CancellationTokenSource();
            shutdown.Cancel();

            var result = await PlatformLauncher.LaunchWithResult(cancellationToken: shutdown.Token);

            Assert.That(result.UnexpectedExit, Is.Null);
        }
    }
}
