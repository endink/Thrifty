using Thrifty.Codecs.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Codecs
{
    public interface IThriftCodec
    {
        /// <summary>
        /// The Thrift type this codec supports.  The Thrift type contains the Java generic Type of the codec.
        /// </summary>
        ThriftType Type { get; }
        Object ReadObject(TProtocol protocol);
        void WriteObject(Object value, TProtocol protocol);

    }

    /// <summary>
    /// A single type codec for reading and writing in Thrift format.  Each codec is symmetric and 
    /// therefore only supports a single concrete type.
    /// <c>Implementations of this interface are expected to be thread safe.</c>
    /// </summary>
    /// <typeparam name="T">the type this codec supports</typeparam>
    public interface IThriftCodec<T> : IThriftCodec
    {
        /// <summary>
        /// Reads a value from supplied Thrift protocol reader. Exception if any problems occurred when reading or coercing the value.
        /// </summary>
        /// <param name="protocol">protocol the protocol to read from</param>
        /// <returns>the value; not null</returns>
        T Read(TProtocol protocol);


        /// <summary>
        /// Writes a value to the supplied Thrift protocol writer.Exception if any problems occurred when writing or coercing the value.
        /// </summary>
        /// <param name="value"> value the value to write; not null</param>
        /// <param name="protocol">protocol the protocol to write to</param>
        void Write(T value, TProtocol protocol);
    }
}
