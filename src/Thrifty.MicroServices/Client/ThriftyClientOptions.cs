using Chopin.Pooling.Impl;
using Microsoft.Extensions.Logging;
using Thrifty.MicroServices.Ribbon;
using Thrifty.Nifty.Client;
using Thrifty.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Thrifty.MicroServices.Client
{
    public class ThriftyClientOptions
    {
        private ThriftyClientEurekaConfig _eurekaConfig = null;
        private IEnumerable<ThriftClientEventHandler> _handlers = null;
        private GenericKeyedObjectPoolConfig _clientPoolOptions = null;
        private IPingStrategy _pingStrategy = null;
        private EndPoint _socketProxy = null;
        private NettyClientConfig _nettyClientConfig = null;

        public string ClientName { get; set; } = "swifty";
        public string ConfigNamespace { get; set; } = "swifty";
        public bool Secure { get; set; } = false;

        public int ConnectTimeoutMilliseconds { get; set; } = 500 * 4;
        public int ReceiveTimeoutMilliseconds { get; set; } = 60000;
        public int ReadTimeoutMilliseconds { get; set; } = 60000;
        public int WriteTimeoutMilliseconds { get; set; } = 30000;
        public int RetriesSameServer { get; set; } = 1;
        public int RetriesNextServer { get; set; } = 5;
        public bool RetryEnabled { get; set; } = true;
        public int MaxFrameSize { get; set; } = 16777216;
        
        public NettyClientConfig NettyClientConfig
        {
            get { return _nettyClientConfig ?? (_nettyClientConfig = new NettyClientConfig()); }
            set { _nettyClientConfig = value; }
        }

        internal string Proxy
        {
            set
            {
                var index = value.IndexOf(":");
                if (index < 0)
                {
                    throw new ArgumentException("proxy value is invalid,format is [ip:port]");
                }
                var host = value.Substring(0, index);
                var isMatch = Regex.IsMatch(host, "^(((2[0-4]\\d)|(25[0-5]))|(1\\d{2})|([1-9]\\d)|(\\d))[.](((2[0-4]\\d)|(25[0-5]))|(1\\d{2})|([1-9]\\d)|(\\d))[.](((2[0-4]\\d)|(25[0-5]))|(1\\d{2})|([1-9]\\d)|(\\d))[.](((2[0-4]\\d)|(25[0-5]))|(1\\d{2})|([1-9]\\d)|(\\d))$");
                if (!isMatch)
                {
                    throw new ArgumentException("host is invalid");
                }
                var port = int.Parse(value.Substring(index + 1));
                if (port < 0)
                {
                    throw new ArgumentException("port is invalid");
                }
                _socketProxy = new DnsEndPoint(host, port);
            }
        }



        internal EndPoint SocketProxy => _socketProxy;

        public IPingStrategy PingStrategy
        {
            get { return _pingStrategy ?? (_pingStrategy = new SerialPingStrategy()); }
            set { _pingStrategy = value; }
        }
        public ILoggerFactory LoggerFactory { get; set; } = null;

        public ThriftyClientEurekaConfig Eureka
        {
            get { return _eurekaConfig ?? (_eurekaConfig = new ThriftyClientEurekaConfig()); }
            set { _eurekaConfig = value; }
        }

        public bool EurekaEnabled { get; set; } = true;

        public IEnumerable<ThriftClientEventHandler> EventHandlers
        {
            get { return _handlers ?? (_handlers = Enumerable.Empty<ThriftClientEventHandler>()); }
            set { _handlers = value; }
        }

        public bool ConnectionPoolEnabled { get; set; } = true;

        public GenericKeyedObjectPoolConfig ConnectionPool
        {
            get { return _clientPoolOptions ?? (_clientPoolOptions = new GenericKeyedObjectPoolConfig() { TestOnBorrow = true, TestOnReturn = true }); }
            set { _clientPoolOptions = value; }
        }


    }
}
