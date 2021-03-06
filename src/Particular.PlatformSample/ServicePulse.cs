﻿namespace Particular
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;

    class ServicePulse
    {
        readonly int port;
        readonly string webroot;

        IWebHost host;
        Task runHostTask;
        CancellationTokenSource shutdown;

        public ServicePulse(int port, string webroot)
        {
            this.port = port;
            this.webroot = webroot;
            shutdown = new CancellationTokenSource();
        }

        public void Run()
        {
            host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(webroot)
                .UseUrls($"http://localhost:{port}")
                .UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True")
                .UseStartup<Startup>()
                .UseWebRoot(webroot)
                .Build();

            runHostTask = host.RunAsync(shutdown.Token);
        }

        public void Stop()
        {
            shutdown.Cancel();
            runHostTask.GetAwaiter().GetResult();
        }

        class Startup
        {
            public void Configure(IApplicationBuilder app)
            {
                app.UseDefaultFiles();
                app.UseStaticFiles();
            }
        }
    }
}
