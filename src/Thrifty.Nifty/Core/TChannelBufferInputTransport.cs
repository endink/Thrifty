using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Transport;

namespace Thrifty.Nifty.Core
{
    public class TChannelBufferInputTransport : TTransport
    {
        private IByteBuffer _inputBuffer;

        public TChannelBufferInputTransport()
        {
            this._inputBuffer = null;
        }

        public TChannelBufferInputTransport(IByteBuffer inputBuffer)
        {
            SetInputBuffer(inputBuffer);
        }

        public override bool IsOpen
        {
            get
            {
                throw new NotSupportedException();
            }
        }

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
            Guard.ArgumentCondition(_inputBuffer != null, "Tried to read before setting an input buffer");
            _inputBuffer.ReadBytes(buf, off, len);
            return len;
        }
        public override void Write(byte[] buf, int off, int len)
        {
            throw new NotSupportedException();
        }

        public void SetInputBuffer(IByteBuffer inputBuffer)
        {
            this._inputBuffer = inputBuffer;
        }

        public bool IsReadable()
        {
            return _inputBuffer.IsReadable();
        }

        protected override void Dispose(bool disposing)
        {
            //if (disposing)
            //{
            //    if ((_inputBuffer?.ReferenceCount ?? 0) > 0)
            //    {
            //        _inputBuffer?.Release();
            //    }
            //    _inputBuffer = null;
            //}
        }
    }
}
