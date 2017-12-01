using Chopin.Pooling.Impl;
using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chopin.Pooling;
using Thrifty.Services;

namespace Thrifty.MicroServices.Client.Pooling
{
    public class NiftyClientChannelPool : GenericKeyedObjectPool<ChannelKey, INiftyClientChannel>, IClientConnectionPool
    {
        public NiftyClientChannelPool(ThriftClientManager thriftClientManager, IEvictionTimer timer, ThriftyClientOptions options) 
            : base(new NiftyClientChannelFactory(thriftClientManager, options), options.ConnectionPool, timer, options.LoggerFactory)
        {

            
        }

        public INiftyClientChannel BorrowChannel(ChannelKey key)
        {
            return this.BorrowObject(key);
        }

        public void ClearChannel(ChannelKey key)
        {
            this.Clear(key);
        }

        public void ReturnChannel(ChannelKey key,INiftyClientChannel channel)
        {
            this.ReturnObject(key,channel);
        }
    }
}
