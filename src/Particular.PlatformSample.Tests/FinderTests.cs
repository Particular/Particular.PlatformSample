namespace Particular.PlatformSample.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class FinderTests
    {
        [Test]
        public void FindsSolutionRoot()
        {
            var root = Finder.FindSolutionRoot();

            StringAssert.EndsWith("src", root);
        }
    }
}