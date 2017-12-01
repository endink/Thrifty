using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    /// <summary>
    /// Marks a field, method or parameter as a Thrift field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property| AttributeTargets.Parameter, AllowMultiple =false, Inherited =true)]
    public sealed class ThriftFieldAttribute : Attribute
    {
        /// <summary>
        /// 创建 <see cref="ThriftFieldAttribute"/> 类的新实例。
        /// </summary>
        /// <param name="id">表示字段的顺序，在一个 struct 内应该保持唯一。</param>
        public ThriftFieldAttribute(short id = short.MinValue)
        {
            this.Id = id;
        }

        /// <summary>
        /// 获取或设置字段的 IDL 名称，如果为 null 或空串序列化时将使用字段名。
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// 获取或设置字段的 IDL 顺序（顺序必要保证一个 struct 内唯一）。
        /// </summary>
        public short Id { get; set; }

        /// <summary>
        ///  获取或设置一个值，指示字段的必要性。
        /// </summary>
        public Requiredness Required { get; set; }

        /// <summary>
        /// 获取或设置一个值，表示字段是否是存在递归的引用。
        /// </summary>
        public Recursiveness Recursive { get; set; }

        public enum Recursiveness
        {
            Unspecified,
            False,
            True,
        }
        public enum Requiredness
        {
            /// <summary>
            /// This is the default (unset) value for <see cref="ThriftFieldAttribute.Required"/>. 
            /// It will not conflict with other explicit settings of <see cref="Requiredness.None"/>, <see cref="Requiredness.Required"/>, 
            /// or <see cref="Requiredness.Optional"/>. If all of the <see cref="ThriftFieldAttribute.Required"/> 
            /// attribute for a field are left<see cref="Requiredness.Unspecified"/>, it determines whether or not the type can be null.
            /// </summary>
            Unspecified,
            /// <summary>
            /// This behavior is equivalent to leaving out 'optional' and 'required' in thrift IDL
            /// syntax. However, despite the name, this actually does correspond to defined behavior so
            /// if this value is explicitly specified in any annotations, it will conflict with other
            /// annotations that specify either <see cref="Requiredness.Optional"/> or <see cref="Requiredness.Required"/> for the same field.
            /// 
            /// The serialization behavior is that <c>null</c> values will not be serialized, but
            /// if the field is non-nullable (i.e. it's type is primitive) it will be serialized, even
            /// if the field was not explicitly set.
            /// 
            /// The deserialization behavior is similar: When no value is read, the field will be set
            /// to <c>null</c> if the type of the field is nullable, but for primitive types the
            /// field will be left untouched (so it will hold the default value for the type).
            /// </summary>
            None,
            /// <summary>
            /// This behavior indicates that the field will always be serialized (and it is an error if the value is <c>null</c>), 
            /// and must always be deserialized (and it is an error if a value is not read).
            /// </summary>
            Required,
            /// <summary>
            /// This behavior indicates that it is always ok if the field is <c>null</c> when serializing, 
            /// and that it is always ok to not read a value (and the field will be set to <c>null</c> when this happens). 
            /// As such, primitive types should be replaced with boxed types, so that null is always a possibility.
            /// </summary>
            Optional
        }
    }
}
