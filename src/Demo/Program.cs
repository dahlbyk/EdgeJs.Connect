using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Diagnostics;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = "http://localhost:12345";
            using (WebApp.Start<Startup>(url))
            {
                if (Environment.UserInteractive)
                    Process.Start(url);

                Console.ReadLine();
            }

        }

        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
#if DEBUG
                app.UseErrorPage();
#endif

                app.UseFunc(
                    next =>
                    async env =>
                    {
                        Console.WriteLine(env["owin.RequestPath"]);

                        await next(env);
                        return;
                    });

                app.UseWelcomePage("/");
            }
        }
    }
}
