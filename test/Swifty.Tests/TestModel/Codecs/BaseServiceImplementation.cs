using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    public class BaseServiceImplementation : IBaseService
    {
        public void FooBase()
        {
            throw new NotImplementedException();
        }
    }
}
