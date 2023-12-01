namespace Particular.PlatformSample.Tests
{
    using System.Linq;
    using System.Net.NetworkInformation;
    using NUnit.Framework;

    [Explicit]
    [TestFixture]
    public class NetworkTests
    {
        [Test]
        public void FindAvailablePorts()
        {
            var ipGlobalProps = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProps.GetActiveTcpListeners();
            var knownTakenPort = listeners.First().Port;

            var results = Network.FindAvailablePorts(knownTakenPort, 5);

            Assert.Multiple(() =>
            {
                Assert.That(results, Has.Length.EqualTo(5));
                Assert.That(results.Distinct().Count(), Is.EqualTo(5));
                Assert.That(results.All(p => p != knownTakenPort), Is.True);
            });
        }
    }
}