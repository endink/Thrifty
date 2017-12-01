using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Thrift.Transport;
using Thrift.Protocol;

namespace Thrifty.Nifty.Core
{
    public class ThriftUnframedDecoder : DotNetty.Codecs.ByteToMessageDecoder
    {

        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            int messageBeginIndex = input.ReaderIndex;
            IByteBuffer messageBuffer = null;

            try
            {
                using (TTransport transport = new TChannelBufferInputTransport(input))
                {
                    using (TBinaryProtocol protocol = new TBinaryProtocol(transport))
                    {
                        protocol.ReadMessageBegin();
                        TProtocolUtil.Skip(protocol, TType.Struct);
                        protocol.ReadMessageEnd();

                        messageBuffer = input.Slice(messageBeginIndex, input.ReaderIndex);
                    }
                }
              
            }
            catch (IndexOutOfRangeException)
            {
                input.SetReaderIndex(messageBeginIndex);
                return;
            }
            catch (Exception ex)
            {
                ex.ThrowIfNecessary();
                input.SetReaderIndex(messageBeginIndex);
                return;
            }

            output.Add(messageBuffer);
        }
    }
}
