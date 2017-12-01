using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Nifty.Codecs
{
    public interface IThriftFrameCodecFactory
    {
        IChannelHandler Create(long maxFrameSize, TProtocolFactory defaultProtocolFactory, ILoggerFactory loggerFactory);
    }
}
