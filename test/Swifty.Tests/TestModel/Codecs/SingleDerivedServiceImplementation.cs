using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    public class SingleDerivedServiceImplementation : IDerivedServiceOne
    {
        public void FooBase()
        {
            throw new NotImplementedException();
        }

        public void FooOne()
        {
            throw new NotImplementedException();
        }
    }
}
