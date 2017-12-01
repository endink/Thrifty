using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    public static class Buffers
    {
        public static IByteBuffer WrappedBuffer(IByteBufferAllocator allocator, params IByteBuffer[] buffers)
        {
            switch (buffers.Length)
            {
                case 0:
                    break;
                case 1:
                    if (buffers[0].IsReadable())
                    {
                        return buffers[0].Slice();
                    }
                    break;
                default:
                    ByteOrder? order = null;
                    List<IByteBuffer> components = new List<IByteBuffer>(buffers.Length);
                    foreach (IByteBuffer c in buffers)
                    {
                        if (c == null)
                        {
                            break;
                        }
                        if (c.IsReadable())
                        {
                            if (order != null)
                            {
                                if (!order.Equals(c.Order))
                                {
                                    throw new ArgumentException(
                                            "inconsistent byte order");
                                }
                            }
                            else
                            {
                                order = c.Order;
                            }
                            if (c is CompositeByteBuffer)
                            {
                                // Expand nested composition.
                                components.AddRange(
                                        ((CompositeByteBuffer)c).Decompose(
                                                c.ReadInt(), c.ReadableBytes));
                            }
                            else
                            {
                                // An ordinary buffer (non-composite)
                                components.Add(c.Slice());
                            }
                        }
                    }
                    return Buffers.CompositeBuffer(order.Value, components, allocator);
            }
            return Unpooled.Empty;
        }

        private static IByteBuffer CompositeBuffer(ByteOrder endianness, List<IByteBuffer> components, IByteBufferAllocator allocator)
        {
            switch (components.Count())
            {
                case 0:
                    return Unpooled.Empty;
                case 1:
                    return components[0];
                default:
                    return new CompositeByteBuffer(allocator, components.Count, components);
            }
        }
    }

    
}
