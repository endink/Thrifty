using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Commons
{
    class ThriftyHealthCheck : IHealthCheck
    {
        public byte Ping() => 1;
    }
}
