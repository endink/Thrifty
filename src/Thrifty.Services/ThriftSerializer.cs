using Thrifty.Codecs;
using System.IO;
using Thrift.Protocol;
using Thrift.Transport;

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

        public enum SerializeProtocol
        {
            Binary,
            Compact,
        }
    }
}
