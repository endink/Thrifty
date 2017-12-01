using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty;
using DotNetty.Transport.Channels;

namespace Thrifty.Nifty.Codecs
{
    /// <summary>
    /// 表示一个 Thrift 帧解析的 <see cref="IChannelHandler"/>
    /// </summary>
    public interface IThriftFrameCodec : IChannelHandler
    {
    }
}
