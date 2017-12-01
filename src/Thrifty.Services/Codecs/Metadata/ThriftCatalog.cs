using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using static Thrifty.Codecs.Metadata.MetadataErrors;
using System.Reflection;
using Thrifty.Codecs.Internal.Coercion;
using Thrifty;
using System.Collections;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftCatalog
    {
        private readonly ConcurrentDictionary<Type, ThriftStructMetadata> _structs = new ConcurrentDictionary<Type, ThriftStructMetadata>();
        private readonly ConcurrentDictionary<Type, TypeCoercion> _coercions = new ConcurrentDictionary<Type, TypeCoercion>();
        private readonly ConcurrentDictionary<Type, ThriftType> _manualTypes = new ConcurrentDictionary<Type, ThriftType>();
        private readonly ConcurrentDictionary<Type, ThriftType> _typeCache = new ConcurrentDictionary<Type, ThriftType>();

        /// <summary>
        /// This stack tracks the java Types for which building a ThriftType is in progress (used to detect recursion)
        /// </summary>
        private ThreadLocal<Stack<Type>> _stack = new ThreadLocal<Stack<Type>>(() => new Stack<Type>());

        private ThreadLocal<Stack<Type>> _deferredTypesWorkList = new ThreadLocal<Stack<Type>>(() => new Stack<Type>());

        /// <summary>
        /// This queue tracks the Types for which resolution was deferred in order to allow for recursive type structures. 
        /// ThriftTypes for these types will be built after the originally requested ThriftType is built and cached.
        /// </summary>
        /// <param name="monitor"></param>
        public ThriftCatalog(IMonitor monitor = null)
        {
            this.Monitor = monitor ?? MetadataErrors.NullMonitor;
            AddDefaultCoercions();
        }

        internal IMonitor Monitor { get; }

        public void AddThriftType(ThriftType thriftType)
        {
            _manualTypes.AddOrUpdate(thriftType.CSharpType, thriftType, (ket, old) => thriftType);
        }



        private void VerifyCoercionMethod(MethodInfo method)
        {
            if (method.GetParameters().Length != 1)
            {
                throw new ThriftyException($"{method.DeclaringType.Name}.{method.Name} must have exactly one parameter.");
            }
            if (method.ReturnType != typeof(void))
            {
                throw new ThriftyException($"{method.DeclaringType.Name}.{method.Name} must have a return value.");
            }
        }

        /// <summary>
        /// Gets the ThriftType for the specified Java type.  The native Thrift type for the Java type will
        /// be inferred from the Java type, and if necessary type coercions will be applied.
        /// </summary>
        /// <param name="csharpType"></param>
        /// <returns>the ThriftType for the specified csharp type; never null</returns>
        public ThriftType GetThriftType(Type csharpType)
        {
            ThriftType thriftType = GetThriftTypeFromCache(csharpType);
            if (thriftType == null)
            {
                thriftType = BuildThriftType(csharpType);
            }
            return thriftType;
        }

        /// <summary>
        /// for nullable types codec.
        /// </summary>
        public void AddDefaultCoercions()
        {
            this._coercions.TryAdd(typeof(bool?), new TypeCoercion(ThriftType.Bool.CoerceTo(typeof(bool?))));
            this._coercions.TryAdd(typeof(byte?), new TypeCoercion(ThriftType.Byte.CoerceTo(typeof(byte?))));
            this._coercions.TryAdd(typeof(double?), new TypeCoercion(ThriftType.Double.CoerceTo(typeof(double?))));
            this._coercions.TryAdd(typeof(float?), new TypeCoercion(ThriftType.Float.CoerceTo(typeof(float?))));
            this._coercions.TryAdd(typeof(short?), new TypeCoercion(ThriftType.I16.CoerceTo(typeof(short?))));
            this._coercions.TryAdd(typeof(int?), new TypeCoercion(ThriftType.I32.CoerceTo(typeof(int?))));
            this._coercions.TryAdd(typeof(long?), new TypeCoercion(ThriftType.I64.CoerceTo(typeof(long?))));
            this._coercions.TryAdd(typeof(DateTime?), new TypeCoercion(ThriftType.DateTime.CoerceTo(typeof(DateTime?))));
            this._coercions.TryAdd(typeof(Guid?), new TypeCoercion(ThriftType.Guid.CoerceTo(typeof(Guid?))));
            this._coercions.TryAdd(typeof(Decimal?), new TypeCoercion(ThriftType.Decimal.CoerceTo(typeof(Decimal?))));
        }

        /// <summary>
        /// Gets the default TypeCoercion (and associated ThriftType) for the specified csharp type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TypeCoercion GetDefaultCoercion(Type type)
        {
            TypeCoercion r = null;
            _coercions.TryGetValue(type, out r);
            return r;
        }



        public ThriftType GetThriftTypeFromCache(Type csharpType)
        {
            ThriftType tt = null;
            _typeCache.TryGetValue(csharpType, out tt);
            return tt;
        }

        private ThriftType BuildThriftType(Type csharpType)
        {
            ThriftType thriftType = _typeCache.GetOrAdd(csharpType, t => BuildThriftTypeInternal(csharpType));

            if (_stack.Value.Count == 0)
            {
                /*
                 * The stack represents the processing of nested types, so when the stack is empty
                 * at this point, we've just finished processing and caching the originally requested
                 * type. There may be some unresolved type references we should revisit now.
                 */
                var unresolvedCSharpTypes = _deferredTypesWorkList.Value;
                do
                {
                    if (unresolvedCSharpTypes.Count == 0)
                    {
                        break;
                    }
                    Type unresolvedType = unresolvedCSharpTypes.Pop();
                    if (!_typeCache.ContainsKey(unresolvedType))
                    {
                        _typeCache.GetOrAdd(unresolvedType, t => BuildThriftTypeInternal(unresolvedType));
                    }
                } while (true);
            }
            return thriftType;
        }

        public static bool IsStructType(Type csharpType)
        {
            var att = csharpType.GetTypeInfo().GetCustomAttribute<ThriftStructAttribute>();
            if (att != null)
            {
                return true;
            }

            return false;
        }

        private IThriftTypeReference GetThriftTypeReference(Type csharpType, Recursiveness recursiveness)
        {
            ThriftType thriftType = GetThriftTypeFromCache(csharpType);
            if (thriftType == null)
            {
                if (recursiveness == Recursiveness.Forced ||
                    (recursiveness == Recursiveness.Allowed && _stack.Value.Contains(csharpType)))
                {
                    // recursion: return an unresolved ThriftTypeReference
                    _deferredTypesWorkList.Value.Push(csharpType);
                    return new RecursiveThriftTypeReference(this, csharpType);
                }
                else
                {
                    thriftType = _typeCache.GetOrAdd(csharpType, t => BuildThriftType(t));
                }
            }
            return new DefaultThriftTypeReference(thriftType);
        }


        public IThriftTypeReference GetCollectionElementThriftTypeReference(Type csharpType)
        {
            // Collection element types are always allowed to be recursive links
            if (IsStructType(csharpType))
            {
                /*
                 * TODO: This gets things working, but is only necessary when this collection is
                 * involved in a recursive chain. Otherwise, it's just introducing unnecessary
                 * references. We should see if we can clean this up.
                 */
                return GetThriftTypeReference(csharpType, Recursiveness.Forced);
            }
            else
            {
                return GetThriftTypeReference(csharpType, Recursiveness.NotAllowed);
            }
        }

        public IThriftTypeReference GetFieldThriftTypeReference(FieldMetadata fieldMetadata)
        {
            bool? isRecursive = fieldMetadata.IsRecursiveReference;

            if (!isRecursive.HasValue)
            {
                throw new ThriftyException(
                        "Field normalization should have set a non-null value for isRecursiveReference");
            }

            return GetThriftTypeReference(fieldMetadata.CSharpType,
                                          isRecursive.Value ? Recursiveness.Forced : Recursiveness.NotAllowed);
        }



        public bool IsNullable(Type rawType)
        {
            return Nullable.GetUnderlyingType(rawType) != null || !rawType.GetTypeInfo().IsValueType;
        }

        private bool IsNullableEnum(Type rawType)
        {
            var underlying = Nullable.GetUnderlyingType(rawType);
            return underlying != null && underlying.GetTypeInfo().IsEnum;
        }

        public bool IsSupportedArrayElementType(Type elementType)
        {
            return elementType.Equals(typeof(Boolean)) ||
                elementType.Equals(typeof(byte)) ||
                elementType.Equals(typeof(short)) ||
                elementType.Equals(typeof(int)) ||
                elementType.Equals(typeof(long)) ||
                elementType.Equals(typeof(double));
        }


    public ThriftProtocolType GetThriftProtocolType(Type rawType)
        {
            if (typeof(bool).Equals(rawType))
            {
                return ThriftProtocolType.Bool;
            }
            if (typeof(byte).Equals(rawType))
            {
                return ThriftProtocolType.Byte;
            }
            if (typeof(short).Equals(rawType))
            {
                return ThriftProtocolType.I16;
            }
            if (typeof(int).Equals(rawType))
            {
                return ThriftProtocolType.I32;
            }
            if (typeof(long).Equals(rawType))
            {
                return ThriftProtocolType.I64;
            }
            if (typeof(double).Equals(rawType))
            {
                return ThriftProtocolType.Double;
            }
            if (typeof(float).Equals(rawType))
            {
                return ThriftProtocolType.Double;
            }
            if (typeof(String).Equals(rawType))
            {
                return ThriftProtocolType.String;
            }
            if (typeof(byte).Equals(rawType))
            {
                return ThriftProtocolType.Byte;
            }
            if (typeof(DateTime).Equals(rawType))
            {
                return ThriftProtocolType.I64;
            }
            if (typeof(Guid).Equals(rawType))
            {
                return ThriftProtocolType.String;
            }
            if (typeof(Decimal).Equals(rawType))
            {
                return ThriftProtocolType.String;
            }
            if (typeof(byte[]).Equals(rawType))
            {
                return ThriftProtocolType.Binary;
            }
            if (rawType.GetTypeInfo().IsEnum)
            {
                return ThriftProtocolType.Enum;
            }

            var dicType = rawType.GetInterfaces().Concat(new Type[] { rawType }).FirstOrDefault(t => t.GetTypeInfo().IsGenericType &&
                       t.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(IDictionary<,>)));

            if (dicType != null)
            {
                return ThriftProtocolType.Map;
            }

            //if (rawType.IsArray) //TODO: 这里应该判断基础类型，使用 Array 协议，不符合 thrift 标准
            //{
            //    return ThriftProtocolType.List;
            //}

            var setType = rawType.GetInterfaces().Concat(new Type[] { rawType }).FirstOrDefault(t => t.GetTypeInfo().IsGenericType &&
                       t.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(ISet<>)));
            if (setType != null)
            {
                return ThriftProtocolType.Set;
            }

            var listType = rawType.GetInterfaces().Concat(new Type[] { rawType }).FirstOrDefault(t => t.GetTypeInfo().IsGenericType &&
                       t.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(IList<>)));
            if (listType != null)
            {
                return ThriftProtocolType.List;
            }

            var arrayType = rawType.GetInterfaces().Concat(new Type[] { rawType }).FirstOrDefault(t => t.GetTypeInfo().IsGenericType &&
                       t.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)));
            if (arrayType != null)
            {
                return ThriftProtocolType.List;
            }

            // The void type is used by service methods and is encoded as an empty struct
            if (typeof(void).IsAssignableFrom(rawType) || typeof(Task).Equals(rawType))
            {
                return ThriftProtocolType.Struct;
            }

            if (IsStructType(rawType))
            {
                return ThriftProtocolType.Struct;
            }

            if (rawType.GetTypeInfo().IsGenericType &&
                           rawType.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(Task<>)))
            {
                Type returnType = rawType.GetTypeInfo().GetGenericArguments().First();
                // TODO: check that we aren't recursing through multiple futures
                // TODO: find a way to restrict this to return values only
                return this.GetThriftProtocolType(returnType);
            }

            // coerce the type if possible
            TypeCoercion coercion = null;
            if (_coercions.TryGetValue(rawType, out coercion))
            {
                return coercion.ThriftType.ProtocolType;
            }

            if (IsNullableEnum(rawType))
            {
                return ThriftProtocolType.Enum;
            }

            return ThriftProtocolType.Unknown;
        }


        // This should ONLY be called from buildThriftType()
        private ThriftType BuildThriftTypeInternal(Type csharpType)
        {
            ThriftType manualType = null;
            Type rawType = csharpType;
            if (_manualTypes.TryGetValue(csharpType, out manualType))
            {
                return manualType;
            }
            if (typeof(bool).Equals(rawType))
            {
                return ThriftType.Bool;
            }
            if (typeof(byte).Equals(rawType))
            {
                return ThriftType.Byte;
            }
            if (typeof(short).Equals(rawType))
            {
                return ThriftType.I16;
            }
            if (typeof(int).Equals(rawType))
            {
                return ThriftType.I32;
            }
            if (typeof(long).Equals(rawType))
            {
                return ThriftType.I64;
            }
            if (typeof(double).Equals(rawType))
            {
                return ThriftType.Double;
            }
            if (typeof(float).Equals(rawType))
            {
                return ThriftType.Float;
            }
            if (typeof(String).Equals(rawType))
            {
                return ThriftType.String;
            }
            if (typeof(Guid).Equals(rawType))
            {
                return ThriftType.Guid;
            }
            if (typeof(DateTime).Equals(rawType))
            {
                return ThriftType.DateTime;
            }
            if (typeof(Decimal).Equals(rawType))
            {
                return ThriftType.Decimal;
            }
            if (typeof(byte[]).Equals(rawType))
            {
                // byte[] is encoded as BINARY and requires a coersion
                return ThriftType.Binary;
            }
            if (rawType.GetTypeInfo().IsEnum)
            {
                return ThriftType.Enum(new ThriftEnumMetadata(rawType));
            }

            var dicType = rawType.GetInterfaces().Concat(new Type[] { rawType }).FirstOrDefault(t => t.GetTypeInfo().IsGenericType &&
                       t.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(IDictionary<,>)));

            if (dicType != null)
            {
                var argTypes = dicType.GetTypeInfo().GetGenericArguments();
                Type mapKeyType = argTypes[0];
                Type mapValueType = argTypes[1];
                return ThriftType.Dictionary(
                    GetMapKeyThriftTypeReference(mapKeyType),
                    GetMapValueThriftTypeReference(mapValueType));
            }

            if (rawType.IsArray)
            {
                var elementType = rawType.GetTypeInfo().GetElementType();

                return ThriftType.Array(GetCollectionElementThriftTypeReference(elementType));
            }

            var setType = rawType.GetInterfaces().Concat(new Type[] { rawType }).FirstOrDefault(t => t.GetTypeInfo().IsGenericType &&
                       t.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(ISet<>)));
            if (setType != null)
            {
                Type elementType = setType.GetTypeInfo().GetGenericArguments().First();
                return ThriftType.Set(GetCollectionElementThriftTypeReference(elementType));
            }

            var listType = rawType.GetInterfaces().Concat(new Type[] { rawType }).FirstOrDefault(t => t.GetTypeInfo().IsGenericType &&
                       t.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(IList<>)));
            if (listType != null)
            {
                Type elementType = listType.GetTypeInfo().GetGenericArguments().First();
                return ThriftType.List(GetCollectionElementThriftTypeReference(elementType));
            }

            var arrayType = rawType.GetInterfaces().Concat(new Type[] { rawType }).FirstOrDefault(t => t.GetTypeInfo().IsGenericType &&
                       t.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)));
            if (arrayType != null)
            {
                Type elementType = arrayType.GetTypeInfo().GetGenericArguments().First();
                return ThriftType.Array(GetCollectionElementThriftTypeReference(elementType));
            }

            // The void type is used by service methods and is encoded as an empty struct
            if (typeof(void).IsAssignableFrom(rawType) || typeof(Task).Equals(rawType))
            {
                return ThriftType.Void;
            }
            if (IsStructType(rawType))
            {
                ThriftStructMetadata structMetadata = GetThriftStructMetadata(csharpType);
                // Unions are covered because a union looks like a struct with a single field.
                return ThriftType.Struct(structMetadata);
            }

            if(rawType.GetTypeInfo().IsGenericType &&
                           rawType.GetTypeInfo().GetGenericTypeDefinition().Equals(typeof(Task<>)))
            {
                Type returnType = rawType.GetTypeInfo().GetGenericArguments().First();
                // TODO: check that we aren't recursing through multiple futures
                // TODO: find a way to restrict this to return values only
                return GetThriftType(returnType);
            }

            // coerce the type if possible
            TypeCoercion coercion = null;
            if (_coercions.TryGetValue(csharpType, out coercion))
            {
                return coercion.ThriftType;
            }

            if (IsNullableEnum(rawType))
            {
                coercion = _coercions.GetOrAdd(rawType, t =>
                {
                    ThriftEnumMetadata m = new ThriftEnumMetadata(t.GetTypeInfo().GetGenericArguments().First());
                    var tt = ThriftType.Enum(m, true);
                    return new TypeCoercion(tt);
                });
                return coercion.ThriftType;
            }

            throw new ThriftyException($"Type can not be coerced to a Thrift type: {csharpType.FullName}");
        }


        public IThriftTypeReference GetMapKeyThriftTypeReference(Type csharpType)
        {
            // Maps key types are always allowed to be recursive links
            if (IsStructType(csharpType))
            {
                /*
                 * TODO: This gets things working, but is only necessary when this collection is
                 * involved in a recursive chain. Otherwise, it's just introducing unnecessary
                 * references. We should see if we can clean this up.
                 */
                return GetThriftTypeReference(csharpType, Recursiveness.Forced);
            }
            else
            {
                return GetThriftTypeReference(csharpType, Recursiveness.NotAllowed);
            }
        }

        public IThriftTypeReference GetMapValueThriftTypeReference(Type csharpType)
        {
            // Maps value types are always allowed to be recursive links
            if (IsStructType(csharpType))
            {
                /*
                 * TODO: This gets things working, but is only necessary when this collection is
                 * involved in a recursive chain. Otherwise, it's just introducing unnecessary
                 * references. We should see if we can clean this up.
                 */
                return GetThriftTypeReference(csharpType, Recursiveness.Forced);
            }
            else
            {
                return GetThriftTypeReference(csharpType, Recursiveness.NotAllowed);
            }
        }



        public bool IsSupportedStructFieldType(Type csharpType)
        {
            return GetThriftProtocolType(csharpType) != ThriftProtocolType.Unknown;
        }



        /// <summary>
        /// Gets the ThriftStructMetadata for the specified struct class.  The struct class must be annotated with @ThriftStruct or @ThriftUnion.
        /// </summary>
        /// <param name="structType"></param>
        /// <returns></returns>
        public ThriftStructMetadata GetThriftStructMetadata(Type structType)
        {
            return _structs.GetOrAdd(structType, t =>
            {
                ThriftStructAttribute att = t.GetTypeInfo().GetCustomAttribute<ThriftStructAttribute>();
                if (att != null)
                {
                    return ExtractThriftStructMetadata(t);
                }
                else
                {
                    throw new ArgumentException($"{structType.FullName} has no ThriftStructAttribute.");
                }
            });
        }


        private ThriftStructMetadata ExtractThriftStructMetadata(Type structType)
        {
            Guard.ArgumentNotNull(structType, nameof(structType));

            var stack = this._stack.Value;
            if (stack.Contains(structType))
            {
                string path = String.Join("->", stack.Union(new Type[] { structType })
                     .Select(t => t.Name));

                throw new ThriftyException(
                    $"Circular references must be qualified with '{nameof(ThriftFieldAttribute.Recursive)}' on a ThriftFieldAttribute in the cycle: {path}.");
            }
            else
            {
                stack.Push(structType);

                try
                {
                    ThriftStructMetadataBuilder builder = new ThriftStructMetadataBuilder(this, structType);
                    ThriftStructMetadata structMetadata = builder.Build();
                    return structMetadata;
                }
                finally
                {
                    Type top = stack.Pop();
                    if (!structType.Equals(top))
                    {
                        throw new ThriftyException($"ThriftCatalog circularity detection stack is corrupt: expected {structType.FullName}, but got {top.FullName}");
                    }
                }
            }
        }

        public static int? GetMethodOrder(MethodInfo method)
        {
            var order = method.GetCustomAttribute<ThriftOrderAttribute>();
            return order?.Order;
        }


        enum Recursiveness
        {
            NotAllowed,
            Allowed,
            Forced,
        }
    }
}
