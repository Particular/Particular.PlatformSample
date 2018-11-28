namespace Particular.PlatformSample.Tests
{
    using System;
    using System.Threading;
    using NUnit.Framework;

    [Explicit]
    [TestFixture]
    public class PlatformLauncherTests
    {
        [Test]
        public void Launch()
        {
            PlatformLauncher.LaunchInternal(Console.Out, Console.In, () =>
            {
                Console.WriteLine("Platform is launched, waiting 1s");
                Thread.Sleep(1000);
                Console.WriteLine("Shutting down platform");
            });
        }
    }
}