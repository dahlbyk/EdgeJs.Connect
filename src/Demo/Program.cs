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
            private readonly IDictionary<string, object> env;
            private readonly AppFunc next;

            public Context(IDictionary<string, object> env, AppFunc next)
            {
                this.env = env;
                this.next = next;
            }

            public RequestProxy Request
            {
                get
                {
                    return new RequestProxy(env);
                }
            }

            public ResponseProxy Response
            {
                get
                {
                    return new ResponseProxy(env);
                }
            }

            public Func<object, Task<object>> Next
            {
                get
                {
                    return async _ =>
                    {
                        await next(env);
                        return null;
                    };
                }
            }

            public string Path
            {
                get { return (string)env["owin.RequestPath"]; }
            }

            public Func<object, Task<object>> WriteText
            {
                get
                {
                    return async input =>
                    {
                        return await env.WriteText(input);
                    };
                }
            }

            public class RequestProxy
            {
                private readonly IDictionary<string, object> env;

                public RequestProxy(IDictionary<string, object> env)
                {
                    this.env = env;

                    var headers = (IDictionary<string, string[]>)env["owin.RequestHeaders"];
                    var uri =
                       (string)env["owin.RequestScheme"] +
                       "://" +
                       headers["Host"].First() +
                       (string)env["owin.RequestPathBase"] +
                       (string)env["owin.RequestPath"];

                    if ((string)env["owin.RequestQueryString"] != "")
                    {
                        uri += "?" + (string)env["owin.RequestQueryString"];
                    }
                    Url = uri;
                }

                public string Url { get; set; }
            }

            public class ResponseProxy
            {
                private readonly IDictionary<string, object> env;

                public ResponseProxy(IDictionary<string, object> env)
                {
                    this.env = env;
                }

                public Func<object, Task<object>> SetHeader
                {
                    get
                    {
                        return async input =>
                        {
                            var args = (object[])input;
                            env.SetHeader(args[0]?.ToString() ?? "", args[1]?.ToString() ?? "");
                            return null;
                        };
                    }
                }

                public Func<object, Task<object>> End
                {
                    get
                    {
                        return async input =>
                        {
                            var args = (object[])input;
                            return await env.WriteBody(args[0]?.ToString() ?? "");
                        };
                    }
                }

            }
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
                        await connect(new Context(env, next))
                    );

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
                        var sb = new StringBuilder("Environment:\n");

                        foreach (var kvp in env.OrderBy(x => x.Key))
                            sb.AppendFormat("  {0}: {1}\n", kvp.Key, kvp.Value);

                        var content = sb.ToString();
                        await env.WriteText(content);
                    };
            }
        }
    }

    public static class Ext
    {
        public static void SetHeader(this IDictionary<string, object> env, string key, string value)
        {
            var headers = (IDictionary<string, string[]>)env["owin.ResponseHeaders"];
            headers[key] = new[] { value };
        }

        public static async Task<string> WriteText(this IDictionary<string, object> env, object content)
        {
            var text = content?.ToString() ?? "";
            env.SetHeader("Content-Length", text.Length.ToString());
            env.SetHeader("Content-Type", "text/plain");
            return await env.WriteBody(text);
        }

        public static async Task<string> WriteBody(this IDictionary<string, object> env, string text)
        {
            using (var sw = new StreamWriter((Stream)env["owin.ResponseBody"]))
            {
                await sw.WriteAsync(text);
                return text;
            }
        }
    }
}
