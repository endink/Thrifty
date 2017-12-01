using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Transport;

namespace Thrifty.Nifty.Core
{
    public class TChannelBufferOutputTransport : TTransport
    {
        private const int DefaultMinimumSize = 1024;

        // This threshold sets how many times the buffer must be under-utilized before we'll
        // reclaim some memory by reallocating it with half the current size
        private const int UnderUseThreshold = 5;

        private IByteBuffer _outputBuffer;
        private readonly int _minimumSize;
        private int _bufferUnderUsedCounter;

        public TChannelBufferOutputTransport(int minimumSize = DefaultMinimumSize)
        {
            this._minimumSize = Math.Min(DefaultMinimumSize, minimumSize);
            _outputBuffer = Unpooled.UnreleasableBuffer(Unpooled.Buffer(this._minimumSize));
        }

        public override bool IsOpen { get { return true; } }

        public override void Open()
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buf, int off, int len)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buf, int off, int len)
        {
            _outputBuffer.WriteBytes(buf, off, len);
        }

        /*
         * Resets the state of this transport so it can be used to write more messages
         */
        public void ResetOutputBuffer()
        {
            int shrunkenSize = ShrinkBufferSize();

            if (_outputBuffer.WriterIndex < shrunkenSize)
            {
                // Less than the shrunken size of the buffer was actually used, so increment
                // the under-use counter
                ++_bufferUnderUsedCounter;
            }
            else
            {
                // More than the shrunken size of the buffer was actually used, reset
                // the counter so we won't shrink the buffer soon
                _bufferUnderUsedCounter = 0;
            }

            if (ShouldShrinkBuffer())
            {
                if ((_outputBuffer?.ReferenceCount ?? 0) > 0)
                {
                    _outputBuffer?.Release();
                }
                _outputBuffer = Unpooled.Buffer(shrunkenSize);
                _bufferUnderUsedCounter = 0;
            }
            else
            {
                _outputBuffer.Clear();
            }
        }

        public IByteBuffer OutputBuffer
        {
            get { return this._outputBuffer; }
        }

        /*
         * Checks whether we should shrink the buffer, which should happen if we've under-used it
         * UNDER_USE_THRESHOLD times in a row
         */
        private bool ShouldShrinkBuffer()
        {
            // We want to shrink the buffer if it has been under-utilized UNDER_USE_THRESHOLD
            // times in a row, and the size after shrinking would not be smaller than the minimum size
            return _bufferUnderUsedCounter > UnderUseThreshold &&
                   ShrinkBufferSize() >= _minimumSize;
        }

        /*
         * Returns the size the buffer will be if we shrink it
         */
        private int ShrinkBufferSize()
        {
            return _outputBuffer.Capacity >> 1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if ((this._outputBuffer?.ReferenceCount ?? 0) > 0)
                {
                    this._outputBuffer?.Release();
                }
                this._outputBuffer = null;
            }
        }
    }

}
