using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Samples.Thrifty
{
    using global::Thrifty.MicroServices;
    [ThriftService("Scribe2")]
    public interface IScribe2
    {
        [ThriftMethod]
        List<LogEntry> getMessages();

        [ThriftMethod]
        ResultCode log(List<LogEntry> messages);
    }

    public class ScribeTest2 : IScribe2
    {
        public List<LogEntry> getMessages()
        {
            return new List<LogEntry>
            {
                new LogEntry { Category = "c1", Message = Guid.NewGuid().ToString() },
                new LogEntry { Category = "c2", Message = Guid.NewGuid().ToString() },
                new LogEntry { Category = "c3", Message = Guid.NewGuid().ToString() }

            };
        }

        public ResultCode log(List<LogEntry> messages)
        {
            return ResultCode.TRY_LATER;
        }
    }
}
