using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Thrift.Protocol;
using Microsoft.Extensions.Logging;

namespace Thrifty.Nifty.Codecs
{
    public class DefaultThriftFrameCodecFactory : IThriftFrameCodecFactory
    {
        public IChannelHandler Create(long maxFrameSize, TProtocolFactory defaultProtocolFactory, ILoggerFactory loggerFactory)
        {
            defaultProtocolFactory = defaultProtocolFactory ?? new TBinaryProtocol.Factory();
            return new DefaultThriftFrameCodec(maxFrameSize, defaultProtocolFactory);
        }
    }
}
