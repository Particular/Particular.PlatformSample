namespace Particular
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    static class Network
    {
        public static int[] FindAvailablePorts(int startingPort, int count)
        {
            var results = new int[count];
            var ipGlobalProps = IPGlobalProperties.GetIPGlobalProperties();

            var portsInUse = ipGlobalProps.GetActiveTcpListeners()
                .Select(ipEndpoint => ipEndpoint.Port);

            var hashSet = new HashSet<int>(portsInUse);

            for (var i = 0; i < count; i++)
            {
                while (hashSet.Contains(startingPort) || !TestPort(startingPort))
                {
                    startingPort++;
                }

                results[i] = startingPort;
                startingPort++;
            }

            return results;
        }

        static bool TestPort(int port)
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{port}/");

            try
            {
                httpListener.Start();
                httpListener.Stop();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task WaitForHttpOk(string url, int timeoutMilliseconds = 1000, CancellationToken cancellationToken = default)
        {
            var status = HttpStatusCode.Ambiguous;
            var c = new HttpClient();

            while (!cancellationToken.IsCancellationRequested && status != HttpStatusCode.OK)
            {
                try
                {
                    Console.Write(".");
                    var response = await c.GetAsync(url, cancellationToken)
                        .ConfigureAwait(false);

                    status = response.StatusCode;
                }
                catch (Exception ex) when (ex is not OperationCanceledException && !cancellationToken.IsCancellationRequested)
                {
                    if (ex is WebException wx && wx.Response is HttpWebResponse response)
                    {
                        status = response.StatusCode;
                    }
                    else
                    {
                        status = HttpStatusCode.Ambiguous;
                    }
                    await Task.Delay(timeoutMilliseconds, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            Console.WriteLine();
        }
    }
}