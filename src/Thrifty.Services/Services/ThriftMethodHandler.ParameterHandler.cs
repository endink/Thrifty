using Thrifty.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    partial class ThriftMethodHandler
    {
        public class ParameterHandler
        {
            internal ParameterHandler(short id, String name, IThriftCodec codec)
            {
                this.Id = id;
                this.Name = name;
                this.Codec = codec;
            }

            public short Id { get; }

            public string Name { get; }

            public IThriftCodec Codec { get; }
        }
    }
}
