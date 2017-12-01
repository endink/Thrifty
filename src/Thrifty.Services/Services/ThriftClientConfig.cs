using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public class ThriftClientConfig
    {
        public static readonly TimeSpan DEFAULT_CONNECT_TIMEOUT = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan DEFAULT_RECEIVE_TIMEOUT = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan DEFAULT_READ_TIMEOUT = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DEFAULT_WRITE_TIMEOUT = TimeSpan.FromMinutes(1);
        // Default max frame size of 16 MB
        public static readonly int DEFAULT_MAX_FRAME_SIZE = 16777216;

        public int MaxFrameSize { get; set; } = DEFAULT_MAX_FRAME_SIZE;
        public TimeSpan ConnectTimeout { get; set; } = DEFAULT_CONNECT_TIMEOUT;
        public TimeSpan ReceiveTimeout { get; set; } = DEFAULT_RECEIVE_TIMEOUT;
        public TimeSpan ReadTimeout { get; set; } = DEFAULT_READ_TIMEOUT;
        public TimeSpan WriteTimeout { get; set; } = DEFAULT_WRITE_TIMEOUT;
        public EndPoint SocksProxy { get; set; }

        public ClientSslConfig SslConfig { get; set; }

        public static ThriftClientConfig CreateForDebug()
        {
            return new ThriftClientConfig
            {
                ConnectTimeout = TimeSpan.FromMinutes(5),
                ReceiveTimeout = TimeSpan.FromMinutes(5),
                ReadTimeout = TimeSpan.FromMinutes(5),
                WriteTimeout = TimeSpan.FromMinutes(5)
            };
        }
    }
}
