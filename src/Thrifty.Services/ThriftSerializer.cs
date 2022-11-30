using Thrifty.Codecs;
using System.IO;
using Thrift.Protocol;
using Thrift.Transport;
using System;

namespace Thrifty.Services
{
    public sealed class ThriftSerializer
    {
        private readonly ThriftCodecManager _thriftCodecManager;

        public ThriftSerializer(SerializeProtocol protocol = SerializeProtocol.Binary)
        {
            _thriftCodecManager = new ThriftCodecManager();
            this.Protocol = protocol;
        }

        public SerializeProtocol Protocol { get; }

        private TProtocol GetProtocol(TTransport transport)
        {
            switch (this.Protocol)
            {
                case SerializeProtocol.Compact:
                    return new TCompactProtocol(transport);
                case SerializeProtocol.Binary:
                default:
                    return new TBinaryProtocol(transport);
            }
        }

        public byte[] Serialize<TObject>(TObject s)
        {
            byte[] objectBytes = null;
            var codec = _thriftCodecManager.GetCodec(typeof(TObject)) as IThriftCodec;
            using (var outs = new MemoryStream())
            {
                using (var tt = new TStreamTransport(null, outs))
                {
                    using (var protocol = this.GetProtocol(tt))
                    {
                        codec.WriteObject(s, protocol);
                        objectBytes = outs.ToArray();
                    }
                }
            }
            return objectBytes;
        }

        public byte[] Serialize(object s, Type type)
        {
            byte[] objectBytes = null;
            var codec = _thriftCodecManager.GetCodec(type) as IThriftCodec;
            using (var outs = new MemoryStream())
            {
                using (var tt = new TStreamTransport(null, outs))
                {
                    using (var protocol = this.GetProtocol(tt))
                    {
                        codec.WriteObject(s, protocol);
                        objectBytes = outs.ToArray();
                    }
                }
            }
            return objectBytes;
        }

        public byte[] Serialize<TObject>(TObject s, TMessage message)
        {
            byte[] objectBytes = null;
            var codec = _thriftCodecManager.GetCodec(typeof(TObject)) as IThriftCodec;
            using (var outs = new MemoryStream())
            {
                using (var tt = new TStreamTransport(null, outs))
                {
                    using (var protocol = this.GetProtocol(tt))
                    {
                        protocol.WriteMessageBegin(message);
                        codec.WriteObject(s, protocol);
                        objectBytes = outs.ToArray();
                    }
                }
            }
            return objectBytes;
        }

        public byte[] Serialize(object s, Type type, TMessage message)
        {
            byte[] objectBytes = null;
            var codec = _thriftCodecManager.GetCodec(type) as IThriftCodec;
            using (var outs = new MemoryStream())
            {
                using (var tt = new TStreamTransport(null, outs))
                {
                    using (var protocol = this.GetProtocol(tt))
                    {
                        protocol.WriteMessageBegin(message);
                        codec.WriteObject(s, protocol);
                        objectBytes = outs.ToArray();
                    }
                }
            }
            return objectBytes;
        }

        public TObject Deserialize<TObject>(byte[] objectBytes)
        {
            var codec = _thriftCodecManager.GetCodec(typeof(TObject)) as IThriftCodec;
            using (var ins = new MemoryStream(objectBytes))
            {
                using (var tt = new TStreamTransport(ins, null))
                {
                    using (var protocol = this.GetProtocol(tt))
                    {
                        return (TObject)codec.ReadObject(protocol);
                    }
                }
            }
        }

        public object Deserialize(byte[] objectBytes, Type type)
        {
            var codec = _thriftCodecManager.GetCodec(type) as IThriftCodec;
            using (var ins = new MemoryStream(objectBytes))
            {
                using (var tt = new TStreamTransport(ins, null))
                {
                    using (var protocol = this.GetProtocol(tt))
                    {
                        return codec.ReadObject(protocol);
                    }
                }
            }
        }

        public TObject Deserialize<TObject>(byte[] objectBytes, out TMessage message)
        {
            var codec = _thriftCodecManager.GetCodec(typeof(TObject)) as IThriftCodec;
            using (var ins = new MemoryStream(objectBytes))
            {
                using (var tt = new TStreamTransport(ins, null))
                {
                    using (var protocol = this.GetProtocol(tt))
                    {
                        message = protocol.ReadMessageBegin();
                        return (TObject)codec.ReadObject(protocol);
                    }
                }
            }
        }

        public object Deserialize(byte[] objectBytes, Type type, out TMessage message)
        {
            var codec = _thriftCodecManager.GetCodec(type) as IThriftCodec;
            using (var ins = new MemoryStream(objectBytes))
            {
                using (var tt = new TStreamTransport(ins, null))
                {
                    using (var protocol = this.GetProtocol(tt))
                    {
                        message = protocol.ReadMessageBegin();
                        return codec.ReadObject(protocol);
                    }
                }
            }
        }

        public enum SerializeProtocol
        {
            Binary,
            Compact,
        }
    }
}
