using EdgeJs;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AppFunc = System.Func<
                    System.Collections.Generic.IDictionary<string, object>,
                    System.Threading.Tasks.Task
                    >;

namespace EdgeJs.Connect.Demo
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

        public class Context
        {
            public Func<object, Task<dynamic>> Next { get; internal set; }
            public string Path { get; set; }
        }

        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                var connect = Connect();

#if DEBUG
                app.UseErrorPage();
#endif

                app.UseFunc(
                    next =>
                    async env =>
                    {
                        var res = await connect(new Context
                        {
                            Path = (string)env["owin.RequestPath"],
                            Next = async _ => { await next(env); return null; },
                        });
                    });

                app.UseWelcomePage("/");

                app.UseFunc(EnvironmentEndpoint);
            }

            private Func<object, Task<dynamic>> Connect()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "EdgeJs.Connect.Demo.shim.js";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    return Edge.Func(result + "; return module.exports");
                }
            }

            static AppFunc EnvironmentEndpoint(AppFunc ignored)
            {
                return async env =>
                    {
                        using (var sw = new StreamWriter((Stream)env["owin.ResponseBody"]))
                        {
                            var sb = new StringBuilder("Environment:\n");

                            foreach (var kvp in env.OrderBy(x => x.Key))
                                sb.AppendFormat("  {0}: {1}\n", kvp.Key, kvp.Value);

                            var content = sb.ToString();

                            var headers = (IDictionary<string, string[]>)env["owin.ResponseHeaders"];
                            headers["Content-Length"] = new[] { content.Length.ToString() };
                            headers["Content-Type"] = new[] { "text/plain" };
                            await sw.WriteAsync(content);
                        }
                    };
            }
        }
    }
}
