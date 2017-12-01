using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services
{
    [ThriftService("SimpleService")]
    public class SimpleService
    {
        [ThriftMethod]
        public void Sleep(int seconds)
        {
            Thread.Sleep(seconds * 1000);
        }
    }
}
