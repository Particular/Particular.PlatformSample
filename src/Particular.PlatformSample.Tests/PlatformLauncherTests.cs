namespace Particular.PlatformSample.Tests
{
    using System;
    using System.IO;
    using NUnit.Framework;

    [Explicit]
    [TestFixture]
    public class PlatformLauncherTests
    {
        [Test]
        public void Launch()
        {
            PlatformLauncher.Launch(Console.Out, new StringReader(Environment.NewLine));
        }
    }
}