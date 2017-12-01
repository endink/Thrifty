using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftService("DerivedServiceOne")]
    public interface IDerivedServiceOne : IBaseService
    {
        [ThriftMethod]
        void FooOne();
    }
}
