using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.MicroServices.Ribbon;

namespace Thrifty.Tests.Services.MicroServices
{
    internal class EasyHttpPing : IPing
    {
        private readonly int _httpClientTimeout;
        private readonly string _urlPath;
        private readonly ILogger _logger;
        public EasyHttpPing(ILoggerFactory factory, int httpClientTimeout = 5, string urlPath = "/") //默认5秒的超时 
        {
            if (httpClientTimeout < 0)
                throw new ArgumentException("httpClientTimeout必须大于0", nameof(httpClientTimeout));
            _httpClientTimeout = httpClientTimeout;
            _urlPath = urlPath;
            _logger = factory?.CreateLogger(typeof(EasyHttpPing)) ?? NullLogger.Instance;
        }
        private async Task<bool> Connect(Server server, string urlPath)
        {
            var url = $"http://{server}{urlPath}";
            _logger.LogDebug($"Trying URL:{url}");
            try
            {
                using (var client = new HttpClient { Timeout = new TimeSpan(0, 0, _httpClientTimeout) })
                {
                    var x = await client.GetAsync(url);
                    return x.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError($"Try URL Error:{url}", exception);
                return false;
            }
        }
        public bool IsAlive(Server server) => Connect(server, _urlPath).GetAwaiter().GetResult();


    }
}
