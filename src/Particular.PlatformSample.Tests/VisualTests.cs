namespace Particular.PlatformSample.Tests;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

[FixtureLifeCycle(LifeCycle.SingleInstance)]
public class VisualTests
{
    Task launcherTask;
    CancellationTokenSource closePlatformTokenSource;

    [SetUp]
    public async Task Setup()
    {
        closePlatformTokenSource = new CancellationTokenSource();

        launcherTask = Task.Run(async () =>
        {
            await PlatformLauncher.Launch(cancellationToken: closePlatformTokenSource.Token);
        });

        using var timeoutTokenSource = new CancellationTokenSource(30_000);
        await Network.WaitForHttpOk("http://localhost:49205", cancellationToken: timeoutTokenSource.Token);
    }

    [TearDown]
    public async Task TearDown()
    {
        await closePlatformTokenSource.CancelAsync();
        await launcherTask;
        closePlatformTokenSource.Dispose();
    }

    [Test]
    public async Task CheckMonitoringPage()
    {
        var driver = new ChromeDriver();
        driver.Navigate().GoToUrl("http://localhost:49205/#/monitoring");
        await Task.Delay(5000);

        var connectionFailedSpans = driver.FindElements(By.CssSelector(".connection-failed"));
        Assert.That(connectionFailedSpans.Count, Is.EqualTo(0));

        var primaryButtons = driver.FindElements(By.CssSelector(".btn.btn-primary"));
        var noEndpointsButton = primaryButtons.Where(b => b.Text.Contains("how to enable endpoint monitoring")).FirstOrDefault();

        Assert.That(noEndpointsButton, Is.Not.Null);
        Assert.That(noEndpointsButton.GetAttribute("href"), Is.EqualTo("https://docs.particular.net/monitoring/metrics/"));
    }
}