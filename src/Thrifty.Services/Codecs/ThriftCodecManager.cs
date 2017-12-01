using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Thrift.Protocol;
using Thrift.Transport;
using System.IO;
using Thrifty.Codecs.Internal;
using Thrifty.Codecs.Internal.Builtin;
using System.Linq.Expressions;
using System.Reflection;
using Thrifty.Codecs.Internal.Coercion;
using Thrifty.Codecs.Internal.Reflection;

namespace Thrifty.Codecs
{
    public class ThriftCodecManager
    {
        private readonly ConcurrentDictionary<ThriftType, IThriftCodec> _typeCodecs;

        ///<summary>
        ///This stack tracks the java Types for which building a ThriftCodec is in progress (used to
        ///detect recursion)
        ///</summary>
        private ThreadLocal<Stack<ThriftType>> _stack;
        private ThreadLocal<Stack<ThriftType>> _deferredTypesWorkList;
        private IThriftCodecFactory _factory = null;

        public ThriftCodecManager(IEnumerable<IThriftCodec> codecs = null, IThriftCodecFactory factory = null, ThriftCatalog catalog = null)
        {
            _typeCodecs = new ConcurrentDictionary<ThriftType, IThriftCodec>();
            _stack = new ThreadLocal<Stack<ThriftType>>(() => new Stack<ThriftType>());
            _deferredTypesWorkList = new ThreadLocal<Stack<ThriftType>>(() => new Stack<ThriftType>());
            this.Catalog = catalog ?? new ThriftCatalog();
            this._factory = factory ?? new ReflectionThriftCodecFactory();


            AddBuiltinCodec(new BooleanThriftCodec());
            AddBuiltinCodec(new ByteThriftCodec());
            AddBuiltinCodec(new ShortThriftCodec());
            AddBuiltinCodec(new IntThriftCodec());
            AddBuiltinCodec(new LongThriftCodec());
            AddBuiltinCodec(new DoubleThriftCodec());
            AddBuiltinCodec(new ByteBufferThriftCodec());
            AddBuiltinCodec(new StringThriftCodec());
            AddBuiltinCodec(new VoidThriftCodec());
            AddBuiltinCodec(new BooleanArrayThriftCodec());
            AddBuiltinCodec(new ShortArrayThriftCodec());
            AddBuiltinCodec(new IntArrayThriftCodec());
            AddBuiltinCodec(new LongArrayThriftCodec());
            AddBuiltinCodec(new DoubleArrayThriftCodec());
            AddBuiltinCodec(new FloatThriftCodec());
            AddBuiltinCodec(new FloatArrayThriftCodec());
            AddBuiltinCodec(new DateTimeThriftCodec());
            AddBuiltinCodec(new DateTimeArrayThriftCodec());
            AddBuiltinCodec(new GuidThriftCodec());
            AddBuiltinCodec(new DecimalThriftCodec());

            var codecArray = codecs ?? Enumerable.Empty<IThriftCodec>();
            foreach (var codec in codecArray)
            {
                AddCodec(codec);
            }
        }
        

        public ThriftCatalog Catalog { get; }

        public IThriftCodec Load(ThriftType type)
        {
            try
            {
                // When we need to load a codec for a type the first time, we push it on the
                // thread-local stack before starting the load, and pop it off afterwards,
                // so that we can detect recursive loads.
                _stack.Value.Push(type);

                switch (type.ProtocolType)
                {
                    case ThriftProtocolType.Struct:
                        {
                            return _factory.GenerateThriftTypeCodec(this, type.StructMetadata);
                        }
                    case ThriftProtocolType.Map:
                        {
                            var codecType = typeof(MapThriftCodec<,>).MakeGenericType(type.KeyTypeReference.CSharpType, type.ValueTypeReference.CSharpType);
                            return (IThriftCodec)Activator.CreateInstance(codecType, type, GetElementCodec(type.KeyTypeReference), GetElementCodec(type.ValueTypeReference));
                        }
                    case ThriftProtocolType.Set:
                        {
                            var codecType = typeof(SetThriftCodec<>).MakeGenericType(type.ValueTypeReference.CSharpType);
                            return (IThriftCodec)Activator.CreateInstance(codecType, type, GetElementCodec(type.ValueTypeReference));
                        }
                    case ThriftProtocolType.List:
                        {
                            var codecType = typeof(ListThriftCodec<>).MakeGenericType(type.ValueTypeReference.CSharpType);
                            return (IThriftCodec)Activator.CreateInstance(codecType, type, GetElementCodec(type.ValueTypeReference));
                        }
                    case ThriftProtocolType.Enum:
                        {
                            var codecType = typeof(EnumThriftCodec<>).MakeGenericType(type.EnumMetadata.EnumType);
                            return (IThriftCodec)Activator.CreateInstance(codecType, type);
                        }
                    default:
                        if (type.IsCoerced)
                        {
                            var codec = GetCodec(type.UncoercedType);
                            TypeCoercion coercion = this.Catalog.GetDefaultCoercion(type.CSharpType);
                            var coercionThriftCodecType = typeof(CoercionThriftCodec<>).MakeGenericType(type.UncoercedType.CSharpType);
                            return (IThriftCodec)Activator.CreateInstance(coercionThriftCodecType, codec, coercion);
                        }
                        else
                        {
                            return GetCodec(type.UncoercedType);
                        }
                        throw new ThriftyException("Unsupported Thrift type " + type);
                }
            }
            finally
            {
                if (_stack.Value.Count > 0)
                {
                    ThriftType top = _stack.Value.Pop();
                    if (!type.Equals(top))
                    {
                        throw new ThriftyException(
                            $"ThriftCatalog circularity detection stack is corrupt: expected {type}, but got {top}");
                    }
                }
            }

        }

        ///<summary>
        ///Adds or replaces the codec associated with the type contained in the codec.  This does not
        ///replace any current users of the existing codec associated with the type.
        ///</summary>
        public void AddCodec(IThriftCodec codec)
        {
            Catalog.AddThriftType(codec.Type);
            this.AddBuiltinCodec(codec);
        }

        ///<summary>
        ///Adds a ThriftCodec to the codec map, but does not register it with the catalog since builtins
        ///should already be registered
        ///</summary>
        private void AddBuiltinCodec(IThriftCodec codec)
        {
            _typeCodecs.AddOrUpdate(codec.Type, codec, (key, old) => codec);
        }

        public IThriftCodec GetElementCodec(IThriftTypeReference thriftTypeReference)
        {
            return GetCodec(thriftTypeReference.Get());
        }

        public IThriftCodec<T> GetCachedCodecIfPresent<T>()
        {
            return GetCachedCodecIfPresent(typeof(T)) as IThriftCodec<T>;
        }

        public IThriftCodec GetCachedCodecIfPresent(ThriftType type)
        {
            return _typeCodecs.GetOrAdd(type, Load);
        }

        internal IThriftCodec GetCachedCodecIfPresent(Type csharpType)
        {
            ThriftType thriftType = Catalog.GetThriftType(csharpType);
            if (thriftType == null)
            {
                throw new ThriftyException($"Unsupported csharp type {csharpType.FullName}");
            }

            return this.GetCachedCodecIfPresent(thriftType);
        }

        public Object GetCodec(Type csharpType)
        {
            ThriftType thriftType = this.Catalog.GetThriftType(csharpType);
            if (thriftType == null)
            {
                throw new ThriftyException($"Unsupported csharp type {csharpType.FullName}");
            }
            return GetCodec(thriftType);
        }

        public IThriftCodec<T> GetCodec<T>()
        {
            return this.GetCodec(typeof(T)) as IThriftCodec<T>;
        }

        public IThriftCodec GetCodec(ThriftType type)
        {
            // The loading function pushes types before they are loaded and pops them afterwards in
            // order to detect recursive loading (which will would otherwise fail in the LoadingCache).
            // In this case, to avoid the cycle, we return a DelegateCodec that points back to this
            // ThriftCodecManager and references the type. When used, the DelegateCodec will require
            // that our cache contain an actual ThriftCodec, but this should not be a problem as
            // it won't be used while we are loading types, and by the time we're done loading the
            // type at the top of the stack, *all* types on the stack should have been loaded and
            // cached.
            if (_stack.Value.Contains(type))
            {
                //性能需要改进？
                var codecType = typeof(DelegateCodec<>).MakeGenericType(type.CSharpType);
                return (IThriftCodec)System.Activator.CreateInstance(codecType, this);
            }
            var thriftCodec = _typeCodecs.GetOrAdd(type, Load);

            while (_deferredTypesWorkList.Value.Count  > 0)
            {
                var first = _deferredTypesWorkList.Value.Pop();
                GetCodec(first);
            }

            return thriftCodec;

        }

        public T Read<T>(TProtocol protocol)
        {
            return GetCodec<T>().Read(protocol);
        }


        public T Read<T>(ThriftType type, TProtocol protocol)
        {
            var codec = GetCodec(type) as IThriftCodec<T>;
            return codec.Read(protocol);
        }

        public T Read<T>(byte[] serializedStruct,
                      TProtocolFactory protocolFactory)
        {
            Guard.ArgumentNotNull(serializedStruct, nameof(serializedStruct));

            using (MemoryStream istream = new MemoryStream(serializedStruct))
            {
                using (TStreamTransport resultIOStream = new TStreamTransport(istream, null))
                {
                    using (TProtocol resultProtocolBuffer = protocolFactory.GetProtocol(resultIOStream))
                    {
                        return Read<T>(resultProtocolBuffer);
                    }
                }
            }
        }

        public void Write<T>(T value, TProtocol protocol)
        {
            this.GetCodec<T>().Write(value, protocol);
        }

        public void Write<T>(ThriftType type, T value, TProtocol protocol)
        {
            IThriftCodec<T> codec = GetCodec(type) as IThriftCodec<T>;
            codec.Write(value, protocol);
        }

        public void Write<T>(T tValue,
                          Stream oStream,
                          TProtocolFactory protocolFactory)
        {
            Guard.ArgumentNotNull(tValue, nameof(tValue));
            Guard.ArgumentNotNull(protocolFactory, nameof(protocolFactory));
            Guard.ArgumentNotNull(oStream, nameof(oStream));

            TStreamTransport resultIOStream = new TStreamTransport(null, oStream);
            TProtocol resultProtocolBuffer = protocolFactory.GetProtocol(resultIOStream);
            Write<T>(tValue, resultProtocolBuffer);
        }
    }
}
