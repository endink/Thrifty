using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftService("BaseService")]
    public interface IBaseService
    {
        [ThriftMethod]
        void FooBase();
    }
}
