using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftService("DerivedServiceTwo")]
    public interface IDerivedServiceTwo : IBaseService
    {
        [ThriftMethod]
        void FooTwo();
    }
}
