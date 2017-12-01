using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Ribbon.Test;
using Thrifty.MicroServices.Ribbon;

namespace Thrifty.Tests.Services.MicroServices
{

    public class BaseTest : System.IDisposable
    {
        private class HttpServer : System.IDisposable
        {
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
                    catch (Exception)
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

                }
            }
        }


        private IPing _ping;
        private ILoggerFactory _loggerFactory;
        protected IPing Ping { get { return _ping; } }
        protected ILoggerFactory LoggerFactory { get { return _loggerFactory; } }

        private readonly IList<Server> _servers;

        protected IList<Server> Servers { get { return _servers; } }

        private List<HttpServer> _httpServers;
        public BaseTest()
        {
            var count = 20;
            var start = 57000;
            _loggerFactory = new LoggerFactory();
            _ping = new EasyHttpPing(_loggerFactory);
            _servers = Enumerable.Range(start, count).Select(p => new Server("localhost", p)).ToArray();
            _httpServers = new List<HttpServer>(from server in _servers
                                                where server.Port % 2 == 0
                                                select new HttpServer(server));
            _httpServers.ForEach(x => x.Start());
        }

        public void Dispose()
        {
            if (_httpServers != null)
            {
                _httpServers.ForEach(x => x.Dispose());
            }
        }
    }
}
