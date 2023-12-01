namespace Particular.PlatformSample.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class FinderTests
    {
        [Test]
        public void FindsSolutionRoot()
        {
            var finder = new Finder();

            Assert.That(finder.SolutionRoot, Does.EndWith("src"));
        }
    }
}