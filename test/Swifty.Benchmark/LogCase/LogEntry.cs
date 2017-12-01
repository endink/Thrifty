using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Benchmark
{
    [ThriftStruct]
    public class LogEntry
    {
        [ThriftField(1)]
        public string Category { get; set; }

        [ThriftField(2)]
        public string Message { get; set; }
    }
}
