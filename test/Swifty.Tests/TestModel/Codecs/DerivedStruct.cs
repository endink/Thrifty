using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftStruct]
    public class DerivedStruct : SimpleStruct
    {
        [ThriftField(99)]
        public String PropertyExtend { get; set; }
    }
}
