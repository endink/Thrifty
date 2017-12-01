using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.Services
{
    [ThriftStruct]
    public class LogEntry
    {
        [ThriftField(1)]
        public string Category { get; set; }

        [ThriftField(2)]
        public string Message { get; set; }
        
        [ThriftConstructor]
        public LogEntry([ThriftField(2)]String message, [ThriftField(1)]String category)
        {
            this.Message = message;
            this.Category = category;
        }

        public LogEntry()
        {

        }
    }
}
