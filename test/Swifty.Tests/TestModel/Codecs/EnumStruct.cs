using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Tests.TestModel.Codecs
{
    [ThriftStruct("enumstruct")]
    public class EnumStruct
    {
        [ThriftField(1)]
        public DefaultEnum DefaultEnum { get; set; }
        [ThriftField(2)]
        public ComplexEnum ComplexEnum { get; set; }
        //[ThriftField(3)]
        //public ErrorEnum ErrorEnum { get; set; }
    }

    public enum DefaultEnum
    {
        Node1,
        Node2,
        Node3
    }

    public enum ComplexEnum
    {
        Node1 = 85,
        Node2 = 66,
        Node3 = 13
    }

    public enum ErrorEnum
    {
        Node1 = -22,
        Node2 = -20,
        Node3 = 5,
    }
}
