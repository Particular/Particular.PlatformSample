namespace Particular.PlatformSample.Tests
{
    using NUnit.Framework;

    [Explicit]
    [TestFixture]
    public class PlatformLauncherTests
    {
        [Test]
        public void Launch()
        {
            PlatformLauncher.Launch();
        }
    }
}