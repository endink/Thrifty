using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftStruct]
    public class ListRecursiveStruct
    {
        [ThriftField(1)]
        public IList<Struct2> List { get; set; }
    }

    [ThriftStruct]
    public class Struct2
    {
        [ThriftField(1, Recursive = ThriftFieldAttribute.Recursiveness.True)]
        public ListRecursiveStruct Parent { get; set; }
    }

    [ThriftStruct]
    public class InvalidRecursiveStruct
    {
        [ThriftField(1)]
        public InvalidRecursiveStruct Parent { get; set; }
    }

    [ThriftStruct]
    public class RecursiveStruct
    {
        [ThriftField(1, Recursive = ThriftFieldAttribute.Recursiveness.True)]
        public InvalidRecursiveStruct Parent { get; set; }
    }
}
