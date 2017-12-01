using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Client
{
    public interface INiftyClientConnector<T>
        where T : IRequestChannel
    {

        EndPoint ServerAddress { get; }

        Task<IChannel> ConnectAsync(Bootstrap bootstrap);

        T NewThriftClientChannel(IChannel channel, NettyClientConfig clientConfig);

       void ConfigureChannelPipeline(IChannelPipeline pipeline, int maxFrameSize, NettyClientConfig clientConfig, ClientSslConfig sslConfig);
    }
}
