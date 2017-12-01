using System;
using System.Collections.Generic;
using System.Text;

namespace Thrifty.Benchmark
{
    [ThriftService]
    public interface ISimpleCase
    {
        [ThriftMethod]
        List<LogEntry> GetMessages();

        [ThriftMethod]
        ResultCode Log(List<LogEntry> messages);
    }

    public class LogCase : ISimpleCase
    {
        public List<LogEntry> GetMessages()
        {
            return new List<LogEntry>
            {
                new LogEntry { Category = "c1", Message = Guid.NewGuid().ToString() },
                new LogEntry { Category = "c2", Message = Guid.NewGuid().ToString() },
                new LogEntry { Category = "c3", Message = Guid.NewGuid().ToString() }

            };
        }

        public ResultCode Log(List<LogEntry> messages)
        {
            return ResultCode.TRY_LATER;
        }
    }
}
