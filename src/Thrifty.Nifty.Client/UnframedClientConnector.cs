using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Thrifty.Nifty.Duplex;
using Thrifty.Nifty.Core;

namespace Thrifty.Nifty.Client
{
    public class UnframedClientConnector : AbstractClientConnector<UnframedClientChannel>
    {
        public UnframedClientConnector(EndPoint address) : this(address, UnframedClientConnector.GetDefaultProtocolFactory())
        {
        }

        public UnframedClientConnector(EndPoint address, TDuplexProtocolFactory protocolFactory) : base(address, protocolFactory)
        {
        }

        protected override void OnConfigureChannelPipeline(IChannelPipeline pipeline, int maxFrameSize, NettyClientConfig clientConfig)
        {
            TimeoutHandler.AddToPipeline(pipeline);
            pipeline.AddLast("thriftUnframedDecoder", new ThriftUnframedDecoder());
            //if (clientConfig.sslClientConfiguration() != null)
            //{
            //    pipeline.addFirst("ssl", clientConfig.sslClientConfiguration().createHandler(address));
            //}
        }

        public override UnframedClientChannel NewThriftClientChannel(IChannel nettyChannel, NettyClientConfig clientConfig)
        {
            UnframedClientChannel channel = new UnframedClientChannel(nettyChannel, clientConfig.Timer, this.ProtocolFactory);
            var cp = nettyChannel.Pipeline;
            TimeoutHandler.AddToPipeline(cp);
            cp.AddLast("thriftHandler", channel);
            return channel;
        }
    }
}
