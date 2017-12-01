using System.Linq;
using System.Net;
using DotNetty.Transport.Channels;
using Thrifty.Nifty.Duplex;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DotNetty.Codecs;
using Thrift.Protocol;

namespace Thrifty.Nifty.Client
{
    public class FramedClientConnector : AbstractClientConnector<FramedClientChannel>
    {
        private ILoggerFactory _loggerFactory = null;
        private ILogger _logger = null;
        // TFramedTransport framing appears at the front of the message
        private const int LengthFieldOffset = 0;

        // TFramedTransport framing is four bytes long
        private const int LengthFieldLength = 4;

        // TFramedTransport framing represents message size *not including* framing so no adjustment
        // is necessary
        private const int LengthAdjustment = 0;

        // The client expects to see only the message *without* any framing, this strips it off
        private const int InitialBytesToStrip = LengthFieldLength;

        private static EndPoint GetEndPoint(string hostName, int port)
        {
            var ipEndPoint = new IPEndPoint(
                Dns.GetHostAddressesAsync(hostName).GetAwaiter().GetResult()
                    .First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork), port);
            return ipEndPoint;
        }

        public FramedClientConnector(string hostName, int port, ILoggerFactory loggerFactory = null)
            : this(GetEndPoint(hostName, port), GetDefaultProtocolFactory(), loggerFactory)
        {
        }

        public FramedClientConnector(string hostName, int port, TDuplexProtocolFactory factory, ILoggerFactory loggerFactory = null)
            : this(GetEndPoint(hostName, port), factory, loggerFactory)
        {
        }

        public FramedClientConnector(string hostName, int port, TProtocolFactory tFactory, ILoggerFactory loggerFactory = null)
            : this(GetEndPoint(hostName, port), tFactory, loggerFactory)
        {
        }

        public FramedClientConnector(EndPoint address, ILoggerFactory loggerFactory = null) : this(address,
            FramedClientConnector.GetDefaultProtocolFactory(), loggerFactory)
        {
        }

        public FramedClientConnector(EndPoint address, TProtocolFactory tFactory, ILoggerFactory loggerFactory = null)
            : this(address, TDuplexProtocolFactory.FromSingleFactory(tFactory), loggerFactory)
        {

        }

        public FramedClientConnector(EndPoint address, TDuplexProtocolFactory protocolFactory, ILoggerFactory loggerFactory = null) : base(address, protocolFactory)
        {
            Guard.ArgumentNotNull(address, nameof(address));
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger(this.GetType()) ?? NullLogger.Instance;
        }

        protected override void OnConfigureChannelPipeline(IChannelPipeline pipeline, int maxFrameSize, NettyClientConfig clientConfig)
        {
            pipeline.AddLast("frameEncoder", new LengthFieldPrepender(LengthFieldLength));
            pipeline.AddLast(
                    "frameDecoder",
                    new LengthFieldBasedFrameDecoder(
                            maxFrameSize,
                            LengthFieldOffset,
                            LengthFieldLength,
                            LengthAdjustment,
                            InitialBytesToStrip));
            //if (clientConfig.sslClientConfiguration() != null)
            //{
            //    pipeline.AddFirst("ssl", clientConfig.sslClientConfiguration().createHandler(address));
            //}
        }

        public override FramedClientChannel NewThriftClientChannel(IChannel nettyChannel, NettyClientConfig clientConfig)
        {
            FramedClientChannel channel = new FramedClientChannel(nettyChannel, clientConfig.Timer, ProtocolFactory, this._loggerFactory);
            var cp = nettyChannel.Pipeline;
            TimeoutHandler.AddToPipeline(cp);
            cp.AddLast("thriftHandler", channel);
            return channel;
        }
    }
}
