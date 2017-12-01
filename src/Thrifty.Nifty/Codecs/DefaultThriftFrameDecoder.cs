using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Thrift.Protocol;
using DotNetty.Codecs;
using Thrifty.Nifty.Core;
using Thrift.Transport;

namespace Thrifty.Nifty.Codecs
{
    public class DefaultThriftFrameDecoder : ThriftFrameDecoder
    {
        public const int MessageFrameSize = 4;
        private readonly long _maxFrameSize;
        private readonly TProtocolFactory _inputProtocolFactory;

        public DefaultThriftFrameDecoder(long maxFrameSize, TProtocolFactory inputProtocolFactory)
        {
            this._maxFrameSize = maxFrameSize;
            this._inputProtocolFactory = inputProtocolFactory;
        }

        protected override ThriftMessage Decode(IChannelHandlerContext ctx, IByteBuffer buffer)
        {
            //buffer.WithOrder(ByteOrder.BigEndian);
            if (!buffer.IsReadable())
            {
                return null;
            }

            ushort firstByte = buffer.GetUnsignedShort(0);
            if (firstByte >= 0x80)
            {
                IByteBuffer messageBuffer = this.TryDecodeUnframedMessage(ctx, ctx.Channel, buffer, _inputProtocolFactory);

                if (messageBuffer == null)
                {
                    return null;
                }
                // A non-zero MSB for the first byte of the message implies the message starts with a
                // protocol id (and thus it is unframed).
                return new ThriftMessage(messageBuffer, ThriftTransportType.Unframed);
            }
            else if (buffer.ReadableBytes < MessageFrameSize)
            {
                // Expecting a framed message, but not enough bytes available to read the frame size
                return null;
            }
            else
            {
                IByteBuffer messageBuffer = this.TryDecodeFramedMessage(ctx, ctx.Channel, buffer, true);

                if (messageBuffer == null)
                {
                    return null;
                }
                // Messages with a zero MSB in the first byte are framed messages
                return new ThriftMessage(messageBuffer, ThriftTransportType.Framed);
            }
        }

        protected IByteBuffer TryDecodeFramedMessage(IChannelHandlerContext ctx,
                                                   IChannel channel,
                                                   IByteBuffer buffer,
                                                   bool stripFraming)
        {
            // Framed messages are prefixed by the size of the frame (which doesn't include the
            // framing itself).

            int messageStartReaderIndex = buffer.ReaderIndex;
            int messageContentsOffset;

            if (stripFraming)
            {
                messageContentsOffset = messageStartReaderIndex + MessageFrameSize;
            }
            else
            {
                messageContentsOffset = messageStartReaderIndex;
            }

            // The full message is larger by the size of the frame size prefix
            int messageLength = buffer.GetInt(messageStartReaderIndex) + MessageFrameSize;
            int messageContentsLength = messageStartReaderIndex + messageLength - messageContentsOffset;

            if (messageContentsLength > _maxFrameSize)
            {
                ctx.FireExceptionCaught(
                        new TooLongFrameException("Maximum frame size of " + _maxFrameSize +
                                                  " exceeded")
                );
            }

            if (messageLength == 0)
            {
                // Zero-sized frame: just ignore it and return nothing
                buffer.SetReaderIndex(messageContentsOffset);
                return null;
            }
            else if (buffer.ReadableBytes < messageLength)
            {
                // Full message isn't available yet, return nothing for now
                return null;
            }
            else
            {
                // Full message is available, return it
                IByteBuffer messageBuffer = ExtractFrame(buffer,
                                                           messageContentsOffset,
                                                           messageContentsLength);
                buffer.SetReaderIndex(messageStartReaderIndex + messageLength);
                return messageBuffer;
            }
        }

        protected IByteBuffer TryDecodeUnframedMessage(IChannelHandlerContext ctx,
                                                     IChannel channel,
                                                     IByteBuffer buffer,
                                                     TProtocolFactory inputProtocolFactory)
        {
            // Perform a trial decode, skipping through
            // the fields, to see whether we have an entire message available.

            int messageLength = 0;
            int messageStartReaderIndex = buffer.ReaderIndex;

            try
            {
                using (TNiftyTransport decodeAttemptTransport = new TNiftyTransport(channel, buffer, ThriftTransportType.Unframed))
                {
                    int initialReadBytes = decodeAttemptTransport.GetReadByteCount();
                    using (TProtocol inputProtocol =
                            inputProtocolFactory.GetProtocol(decodeAttemptTransport))
                    {

                        // Skip through the message
                        inputProtocol.ReadMessageBegin();
                        TProtocolUtil.Skip(inputProtocol, TType.Struct);
                        inputProtocol.ReadMessageEnd();

                        messageLength = decodeAttemptTransport.GetReadByteCount() - initialReadBytes;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // No complete message was decoded: ran out of bytes
                return null;
            }
            catch (TTransportException)
            {
                // No complete message was decoded: ran out of bytes
                return null;
            }
            finally
            {
                if (buffer.ReaderIndex - messageStartReaderIndex > _maxFrameSize)
                {
                    ctx.FireExceptionCaught(new TooLongFrameException("Maximum frame size of " + _maxFrameSize + " exceeded"));
                }

                buffer.SetReaderIndex(messageStartReaderIndex);
            }

            if (messageLength <= 0)
            {
                return null;
            }

            // We have a full message in the read buffer, slice it off
            IByteBuffer messageBuffer =
                    ExtractFrame(buffer, messageStartReaderIndex, messageLength);
            buffer.SetReaderIndex(messageStartReaderIndex + messageLength);
            return messageBuffer;
        }

        protected IByteBuffer ExtractFrame(IByteBuffer buffer, int index, int length)
        {
            // Slice should be sufficient here (and avoids the copy in LengthFieldBasedFrameDecoder)
            // because we know no one is going to modify the contents in the read buffers.
            //防止 buffer 被释放，在
            buffer.Retain();
            return buffer.Slice(index, length);
        }
    }
}
