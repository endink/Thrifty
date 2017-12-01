using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Net;

namespace Thrifty.MicroServices.Client
{
    public class ChannelKey : IEquatable<ChannelKey>
    {
        public ChannelKey(IPEndPoint ipEndPoint, ClientSslConfig clientSslConfig, int connectionTimeout, int receiveTimeout, int writeTimeout, int readTimeout)
        {
            IpEndPoint = ipEndPoint;
            ConnectionTimeout = connectionTimeout;
            ReceiveTimeout = receiveTimeout;
            WriteTimeout = writeTimeout;
            ReadTimeout = readTimeout;
            SslConfig = clientSslConfig;
        }

        public IPEndPoint IpEndPoint { get; }
        public int ConnectionTimeout { get; }
        public int ReceiveTimeout { get; }
        public int ReadTimeout { get; }
        public int WriteTimeout { get; }

        public ClientSslConfig SslConfig { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ChannelKey);
        }

        public bool Equals(ChannelKey other)
        {
            return other != null &&
                   EqualityComparer<IPEndPoint>.Default.Equals(IpEndPoint, other.IpEndPoint);
        }

        public override int GetHashCode()
        {
            return 1130004661 + EqualityComparer<IPEndPoint>.Default.GetHashCode(IpEndPoint);
        }

        public static bool operator ==(ChannelKey key1, ChannelKey key2)
        {
            return EqualityComparer<ChannelKey>.Default.Equals(key1, key2);
        }

        public static bool operator !=(ChannelKey key1, ChannelKey key2)
        {
            return !(key1 == key2);
        }
    }
}
