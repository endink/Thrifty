using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrifty.MicroServices.Ribbon;

namespace Ribbon.Test
{
    internal class HttpServer : System.IDisposable
    {
        private static readonly Random random = new Random();
        private readonly Server _server;
        private readonly System.Net.HttpListener _listener;
        private volatile bool _running = false;
        private System.Threading.Thread _thread;
        public HttpServer(Server server)
        {
            _server = server;
            _listener = new System.Net.HttpListener();
        }

        public void Start()
        {
            _listener.AuthenticationSchemes = System.Net.AuthenticationSchemes.Anonymous;
            _listener.Prefixes.Add($"http://{_server}/");
            _listener.Start();
            _running = true;
            _thread = new System.Threading.Thread(Handle);
            _thread.Start();
        }

        private void Handle()
        {
            while (_running)
            { 
                try
                {
                    var context = _listener.GetContext();
                    var dely = random.Next(10, 20);
                    //var tmp = random.Next(0, 100);
                    //if ((tmp > 50 && tmp < 70))
                    //    dely += random.Next(20, 30);
                    if ((_server.Port - 50000) % 3 == 0)
                        dely += random.Next(0, 100);
                    System.Threading.Thread.Sleep(dely);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/html";
                    using (var writer = new System.IO.StreamWriter(context.Response.OutputStream))
                    {
                        writer.Write($@"
<html>
    <head>
      <title>{_server}</title>
    </head>
    <body>
      <div style=""text-align:center;"">
         Hello,Response from {_server}
      </div>
    </body>
</html>
");
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _running = false;
                _listener.Stop();
                _thread.Abort();
            }
            catch
            {
                // ignored
            }
        }
    }
}
