﻿namespace Particular
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;

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

        public static void WaitForHttpOk(string url, int timeoutMilliseconds = 1000, string httpVerb = "HEAD", CancellationToken cancellationToken = default)
        {
            var status = HttpStatusCode.Ambiguous;

            while (!cancellationToken.IsCancellationRequested && status != HttpStatusCode.OK)
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = httpVerb;

                try
                {
                    Console.Write(".");
                    var response = (HttpWebResponse)request.GetResponse();
                    status = response.StatusCode;
                }
                catch (WebException wx)
                {
                    if (wx.Response is HttpWebResponse response)
                    {
                        status = response.StatusCode;
                    }
                    else
                    {
                        status = HttpStatusCode.Ambiguous;
                    }

                    Thread.Sleep(timeoutMilliseconds);
                }
            }

            Console.WriteLine();
        }
    }
}
