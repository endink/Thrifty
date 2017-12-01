using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using System.Net;
using Thrifty.Nifty.Duplex;
using Thrift.Protocol;
using System.Net.Security;
using DotNetty.Handlers.Tls;

namespace Thrifty.Nifty.Client
{
    public abstract partial class AbstractClientConnector<T> : INiftyClientConnector<T>
        where T : IRequestChannel
    {

        public AbstractClientConnector(EndPoint address, TDuplexProtocolFactory protocolFactory)
        {
            this.ServerAddress = address;
            this.ProtocolFactory = protocolFactory;
        }
        
        public EndPoint ServerAddress { get; }

        protected TDuplexProtocolFactory ProtocolFactory { get; }
        private EndPoint ResolvedAddress { get; set; }

        private static EndPoint ResolveAddress(EndPoint address)
        {
            IPEndPoint ipAddress = address as IPEndPoint;
            if (ipAddress != null)
            {
                return new IPEndPoint(ipAddress.Address, ipAddress.Port);
            }
            DnsEndPoint dnsAddress = address as DnsEndPoint;
            if (dnsAddress != null)
            {
                return new DnsEndPoint(dnsAddress.Host, dnsAddress.Port, System.Net.Sockets.AddressFamily.InterNetwork);
            }
            return address;
        }

        public Task<IChannel> ConnectAsync(Bootstrap bootstrap)
        {
            //判断是否是一个客户端。
            //if ((bootstrap is Socks4ClientBootstrap)) {
            //    return bootstrap.ConnectAsync(this.Address);
            //}

            if (this.ResolvedAddress == null)
            {
                this.ResolvedAddress = ResolveAddress(this.ServerAddress);
            }
            return bootstrap.ConnectAsync(this.ResolvedAddress);
        }

        protected static TDuplexProtocolFactory GetDefaultProtocolFactory()
        {
            return TDuplexProtocolFactory.FromSingleFactory(new TBinaryProtocol.Factory());
        }

        public void ConfigureChannelPipeline(IChannelPipeline pipeline, int maxFrameSize, NettyClientConfig clientConfig, ClientSslConfig sslConfig)
        {
            if (sslConfig != null)
            {
                RemoteCertificateValidationCallback validationCallback = sslConfig.ValidateServerCertificate;

                pipeline.AddLast("tls",
                    new TlsHandler(stream => new SslStream(stream, true, validationCallback),
                    new ClientTlsSettings(this.ServerAddress.GetHost())));
            }
            this.OnConfigureChannelPipeline(pipeline, maxFrameSize, clientConfig);
        }

        public abstract T NewThriftClientChannel(IChannel channel, NettyClientConfig clientConfig);

        protected abstract void OnConfigureChannelPipeline(IChannelPipeline pipeline, int maxFrameSize, NettyClientConfig clientConfig);
    }
}
