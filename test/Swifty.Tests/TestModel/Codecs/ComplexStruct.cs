using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftStruct]
    public class ComplexStruct
    {
        [ThriftField(1)]
        public List<SimpleStruct> StructListProperty { get; set; }

        [ThriftField(2)]
        public HashSet<SimpleStruct> StructSetProperty { get; set; }

        [ThriftField(3)]
        public Dictionary<String, SimpleStruct> DictionaryStringKeyProperty { get; set; }

        [ThriftField(4)]
        public Dictionary<int, SimpleStruct> DictionaryIntKeyProperty { get; set; }

        [ThriftField(5)]
        public Dictionary<int, float> SimpleDictionaryProperty1 { get; set; }

        [ThriftField(6)]
        public Dictionary<String, float> SimpleDictionaryProperty2 { get; set; }

        [ThriftField(7)]
        public int[] IntArrayProperty { get; set; }

        [ThriftField(8)]
        public SimpleEnum[] EnumArrayProperty { get; set; }

        [ThriftField(9)]
        public List<SimpleEnum> EnumListProperty { get; set; }

        [ThriftField(10)]
        public HashSet<SimpleEnum> EnumSetProperty { get; set; }

        [ThriftField(11)]
        public Dictionary<String, SimpleEnum> EnumDictionaryProperty { get; set; }
        
        [ThriftField(12)]
        public SimpleStruct Simple { get; set; }
        
        [ThriftField(13)]
        public IEnumerable<float> IEnumerableProperty { get; set; }
        
        [ThriftField(14)]
        public IList<float> IListProperty { get; set; }

        [ThriftField(15)]
        public ISet<float> ISetProperty { get; set; }

        [ThriftField(16)]
        public IDictionary<float, String> IDictionaryProperty { get; set; }

        [ThriftField(17)]
        public SimpleStruct[] StructArrayProperty { get; set; }
    }
}
