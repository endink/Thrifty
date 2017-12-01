using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services.Codecs
{
    [ThriftService("CombinedService")]
    public interface ICombinedService : IDerivedServiceOne, IDerivedServiceTwo
    {
        [ThriftMethod]
        void FooCombined();
    }
}
