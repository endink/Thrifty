using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Transport;

namespace Thrifty.Nifty.Processors
{
    public interface INiftyProcessorFactory
    {
        INiftyProcessor GetProcessor(TTransport transport);
    }

    public class DelegateNiftyProcessorFactory : INiftyProcessorFactory
    {
        private Func<TTransport, INiftyProcessor> _func;
        public DelegateNiftyProcessorFactory(Func<TTransport, INiftyProcessor> func)
        {
            Guard.ArgumentNotNull(func, nameof(func));
            _func = func;
        }

        public INiftyProcessor GetProcessor(TTransport transport)
        {
           return _func(transport);
        }
    }
}
