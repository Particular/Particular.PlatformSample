namespace Particular
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

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
                app.UseMiddleware<UrlRewriteMiddleware>();
                app.UseStaticFiles();
            }
        }

        class UrlRewriteMiddleware
        {
            RequestDelegate next;

            public UrlRewriteMiddleware(RequestDelegate next) => this.next = next;

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
            public async Task Invoke(HttpContext context)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
            {
                var requestPath = context.Request.Path.ToString();

                //HINT: This is needed to handle app.constants.js requests from AngularJS application
                //      and allow users to keep their backed url configurations in the same place
                if (requestPath.StartsWith("/a/js/app.constants.js"))
                {
                    context.Request.Path = new PathString("/js/app.constants.js");
                }
                //HINT: This is needed to handle default route for AngularJS
                //      Can be removed when AngularJS is out
                else if (requestPath.Equals("/a/"))
                {
                    context.Request.Path = new PathString("/a/index.html");
                }
                //HINT: This is needed to handle assets for AngularJS
                //      Can be removed when AngularJS is out
                else if (requestPath.StartsWith("/a/"))
                {
                    //NOP
                }
                //HINT: All urls that do not map to files on the disk should be mapped to /index.html for Vue.js
                else if (!requestPath.StartsWith("/assets/") && !requestPath.Equals("favicon.ico") && !requestPath.StartsWith("/js/"))
                {
                    context.Request.Path = new PathString("/index.html");
                }

                await next(context).ConfigureAwait(false);
            }
        }
    }
}
