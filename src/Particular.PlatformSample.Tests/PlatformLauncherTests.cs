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
                Console.WriteLine("Platform is launched, waiting 10s");
                Thread.Sleep(10000);
                Console.WriteLine("Shutting down platform");
            });
        }
    }
}