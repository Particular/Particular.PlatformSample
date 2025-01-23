namespace Particular.PlatformSample.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Parallelizable(ParallelScope.None)]
public class VisualTests
{
    Task launcherTask;
    CancellationTokenSource closePlatformTokenSource;
    ChromeDriver driver;

    [OneTimeSetUp]
    public async Task Setup()
    {
        closePlatformTokenSource = new CancellationTokenSource();

        launcherTask = Task.Run(async () =>
        {
            await PlatformLauncher.Launch(cancellationToken: closePlatformTokenSource.Token);
        });

        using var timeoutTokenSource = new CancellationTokenSource(60_000);

        while (TestPortsInternal.ServicePulse == 0)
        {
            await Task.Delay(500, closePlatformTokenSource.Token);
        }

        await Network.WaitForHttpOk($"http://localhost:{TestPortsInternal.ServicePulse}", cancellationToken: timeoutTokenSource.Token);

        driver = new ChromeDriver();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await closePlatformTokenSource.CancelAsync();
        await launcherTask;
        closePlatformTokenSource.Dispose();
        driver.Close();
        driver.Dispose();
    }

    [Test]
    public async Task ShouldBeConnected()
    {
        driver.Navigate().GoToUrl($"http://localhost:{TestPortsInternal.ServicePulse}/#/dashboard");
        await Task.Delay(5000);

        var connectionFailedSpans = driver.FindElements(By.CssSelector(".connection-failed"));
        Assert.That(connectionFailedSpans.Count, Is.EqualTo(0));

        var connectionOkSpans = driver.FindElements(By.CssSelector(".pa-connection-success"));
        Assert.That(connectionOkSpans.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task CheckMonitoringPage()
    {
        driver.Navigate().GoToUrl($"http://localhost:{TestPortsInternal.ServicePulse}/#/monitoring");
        await Task.Delay(5000);

        var primaryButtons = driver.FindElements(By.CssSelector(".btn.btn-primary"));
        var noEndpointsButton = primaryButtons.Where(b => b.Text.Contains("how to enable endpoint monitoring")).FirstOrDefault();

        Assert.That(noEndpointsButton, Is.Not.Null);
        Assert.That(noEndpointsButton.GetAttribute("href"), Is.EqualTo("https://docs.particular.net/monitoring/metrics/"));
    }
}