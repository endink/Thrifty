using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Client
{
    internal class NiftyClientChannelPipelineSetup
    {
        private readonly int _maxFrameSize;

        NiftyClientChannelPipelineSetup(int maxFrameSize)
        {
            this._maxFrameSize = maxFrameSize;
        }

        private IChannelPipeline Setup(IChannelPipeline cp)
        {
            cp.AddLast("frameEncoder", new LengthFieldPrepender(4));
            cp.AddLast("frameDecoder", new LengthFieldBasedFrameDecoder(_maxFrameSize, 0, 4, 0, 4));
            return cp;
        }
    }
}
