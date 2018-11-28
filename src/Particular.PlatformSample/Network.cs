namespace Particular
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;

    static class Network
    {
        public static bool IsPortAvailable(int port)
        {
            var ipGlobalProps = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipGlobalProps.GetActiveTcpListeners();

            foreach (var ipEndpoint in listeners)
            {
                if (ipEndpoint.Port == port)
                {
                    return false;
                }
            }

            return true;
        }

        public static int[] FindAvailablePorts(int startingPort, int count)
        {
            var results = new int[count];
            var ipGlobalProps = IPGlobalProperties.GetIPGlobalProperties();

            var portsInUse = ipGlobalProps.GetActiveTcpListeners()
                .Select(ipEndpoint => ipEndpoint.Port);

            var hashSet = new HashSet<int>(portsInUse);

            for (var i = 0; i < count; i++)
            {
                while (hashSet.Contains(startingPort))
                {
                    startingPort++;
                }

                results[i] = startingPort;
                startingPort++;
            }

            return results;
        }

        public static void WaitForHttpOk(string url, int timeoutMilliseconds = 1000, string httpVerb = "HEAD")
        {
            HttpStatusCode status;

            do
            {
                Thread.Sleep(timeoutMilliseconds);
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = httpVerb;
                try
                {
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
                }

                Console.WriteLine(status);
            }
            while (status != HttpStatusCode.OK);
        }
    }
}
