using Thrifty.Nifty.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift;
using Thrift.Protocol;

namespace Thrifty.Nifty.Processors
{
    public class DelegateNiftyProcessor : INiftyProcessor
    {
        private readonly Func<TProtocol, TProtocol, IRequestContext, bool> _func;

        public DelegateNiftyProcessor(Func<TProtocol, TProtocol, IRequestContext, bool> func)
        {
            Guard.ArgumentNotNull(func, nameof(func));
            _func = func;
        }

        public Task<bool> ProcessAsync(TProtocol protocolIn, TProtocol protocolOut, IRequestContext requestContext)
        {
            return Task.Run<bool>(() => _func(protocolIn, protocolOut, requestContext));
        }
    }

    public class DelegateTProcessor : TProcessor
    {
        private readonly Func<TProtocol, TProtocol, bool> _func;

        public DelegateTProcessor(Func<TProtocol, TProtocol, bool> func)
        {
            Guard.ArgumentNotNull(func, nameof(func));
            _func = func;
        }

        public bool Process(TProtocol iprot, TProtocol oprot)
        {
            return _func(iprot, oprot);
        }
    }

}
