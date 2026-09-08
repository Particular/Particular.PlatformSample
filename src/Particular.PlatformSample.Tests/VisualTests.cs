namespace Particular.PlatformSample.Tests;

using System;
using System.IO;
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
            await Task.Delay(1000, closePlatformTokenSource.Token);
        }

        await Network.WaitForHttpOk($"http://localhost:{TestPortsInternal.ServicePulse}", cancellationToken: timeoutTokenSource.Token);

        var chromeOpts = new ChromeOptions();
        chromeOpts.AddArgument("--headless=new");
        chromeOpts.AddArgument("--disable-gpu");
        chromeOpts.AddArgument("--ignore-certificate-errors");
        chromeOpts.AddArgument("--disable-extensions");
        chromeOpts.AddArgument("--no-sandbox");
        chromeOpts.AddArgument("--disable-dev-shm-usage");
        chromeOpts.AddArgument("--disk-cache-size=1");
        chromeOpts.AddArgument("--media-cache-size=1");
        chromeOpts.AddArgument("--incognito");
        chromeOpts.AddArgument("--remote-debugging-port=9222");
        chromeOpts.AddArgument("--aggressive-cache-discard");

        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            var runnerTemp = Environment.GetEnvironmentVariable("RUNNER_TEMP");
            var dataDir = Path.Combine(runnerTemp, "browser-testing");
            Directory.CreateDirectory(dataDir);
            chromeOpts.AddArgument($"--user-data-dir={dataDir}");
        }

        var chromeService = ChromeDriverService.CreateDefaultService();
        driver = new ChromeDriver(chromeService, chromeOpts, TimeSpan.FromSeconds(90));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await closePlatformTokenSource.CancelAsync();
        await launcherTask;
        closePlatformTokenSource.Dispose();
        driver.Close();
        await driver.DisposeAsync();
    }

    [Test, CancelAfter(30_000)]
    public async Task ShouldBeConnected(CancellationToken cancellationToken = default)
    {
        await driver.Navigate().GoToUrlAsync($"http://localhost:{TestPortsInternal.ServicePulse}/#/dashboard");

        await WaitFor(
            () => IsDocumentReady() &&
                  driver.FindElements(By.CssSelector(".connection-failed")).Count == 0,
            failureMessage: "Expected ServicePulse to show a successful connection state on the dashboard.", cancellationToken);

        var connectionFailedSpans = driver.FindElements(By.CssSelector(".connection-failed"));
        Assert.That(connectionFailedSpans.Count, Is.EqualTo(0));
    }

    bool IsDocumentReady()
    {
        if (driver is not IJavaScriptExecutor js)
        {
            return false;
        }

        var readyState = js.ExecuteScript("return document.readyState")?.ToString();
        return string.Equals(readyState, "complete", StringComparison.OrdinalIgnoreCase);
    }

    [Test, CancelAfter(30_000)]
    public async Task CheckMonitoringPage(CancellationToken cancellationToken = default)
    {
        await driver.Navigate().GoToUrlAsync($"http://localhost:{TestPortsInternal.ServicePulse}/#/monitoring");

        await WaitFor(
            () => FindMetricsHelpLink() != null,
            failureMessage: "Expected monitoring page to render a link to metrics setup guidance.", cancellationToken);

        var noEndpointsButton = FindMetricsHelpLink();

        Assert.That(noEndpointsButton, Is.Not.Null);
        var href = noEndpointsButton.GetAttribute("href") ?? string.Empty;
        Assert.That(href, Does.Contain("monitoring/metrics"));
    }

    IWebElement FindMetricsHelpLink()
    {
        var primaryButtons = driver.FindElements(By.CssSelector("a.btn.btn-primary, .btn.btn-primary[href]"));

        return primaryButtons.FirstOrDefault(b =>
        {
            var text = (b.Text ?? string.Empty).ToLowerInvariant();
            var href = (b.GetAttribute("href") ?? string.Empty).ToLowerInvariant();

            return href.Contains("monitoring/metrics") ||
                   (text.Contains("enable") && text.Contains("monitoring")) ||
                   text.Contains("metrics");
        });
    }

    static async Task WaitFor(Func<bool> condition, string failureMessage, CancellationToken cancellationToken)
    {
        Exception lastException = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (condition())
                {
                    return;
                }
                await Task.Delay(250, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                //fall through to raise last exception
            }
            catch (Exception ex) when (ex is WebDriverException or InvalidOperationException)
            {
                lastException = ex;
            }
        }

        if (lastException != null)
        {
            throw new AssertionException($"{failureMessage} Last error: {lastException.Message}");
        }

        throw new AssertionException(failureMessage);
    }
}
