using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Ribbon.Client
{
    public class RequestSpecificRetryHandler : IRetryHandler
    {
        public bool IsRetriableException(Exception exception, bool isSameServer)
        {
            throw new NotImplementedException();
        }

        public bool IsCircuitTrippingException(Exception exception)
        {
            throw new NotImplementedException();
        }

        public int MaxRetriesOnSameServer { get; }
        public int MaxRetriesOnNextServer { get; }
    }
}
