using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs
{
    public class DelegateCodec<T> : IThriftCodec<T>
    {
        private ThriftCodecManager codecManager;

        public DelegateCodec(ThriftCodecManager codecManager)
        {
            this.codecManager = codecManager;
        }

        public ThriftType Type
        {
            get { return GetCodecFromCache().Type; }
        }

        public T Read(TProtocol protocol)
        {
            return GetCodecFromCache().Read(protocol);
        }

        public object ReadObject(TProtocol protocol)
        {
            return this.Read(protocol);
        }

        public void Write(T value, TProtocol protocol)
        {
            GetCodecFromCache().Write(value, protocol);
        }

        public void WriteObject(object value, TProtocol protocol)
        {
            this.Write((T)value, protocol);
        }

        private IThriftCodec<T> GetCodecFromCache()
        {
            IThriftCodec<T> codec = codecManager.GetCachedCodecIfPresent<T>();
            if (codec == null)
            {
                throw new ThriftyException(
                    "Tried to encodec/decode using a DelegateCodec before the target codec was " +
                    "built (likely a bug in recursive type support)");
            }
            return codec;
        }
    }

}
