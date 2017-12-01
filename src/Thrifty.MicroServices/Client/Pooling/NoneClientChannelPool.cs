using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrifty.Nifty.Client;
using Thrifty.Services;

namespace Thrifty.MicroServices.Client.Pooling
{
    public class NoneClientChannelPool : IClientConnectionPool
    {
        private ThriftyClientOptions _options;
        private ThriftClientManager _thriftClientManager;

        public NoneClientChannelPool(ThriftClientManager thriftClientManager, ThriftyClientOptions options)
        {
            Guard.ArgumentNotNull(options, nameof(options));
            Guard.ArgumentNotNull(thriftClientManager, nameof(thriftClientManager));

            _thriftClientManager = thriftClientManager;
            _options = options;
        }

        public INiftyClientChannel BorrowChannel(ChannelKey key)
        {
            var connector = new FramedClientConnector(key.IpEndPoint.Address.ToString(), key.IpEndPoint.Port,
                    _options.LoggerFactory);

            var channel = _thriftClientManager.CreateChannelAsync(connector, TimeSpan.FromMilliseconds(key.ConnectionTimeout),
                TimeSpan.FromMilliseconds(key.ReceiveTimeout), TimeSpan.FromMilliseconds(key.ReadTimeout),
                TimeSpan.FromMilliseconds(key.WriteTimeout), _options.MaxFrameSize, key.SslConfig, _options.SocketProxy);

            return channel.GetAwaiter().GetResult();
        }

        public void ReturnChannel(ChannelKey key, INiftyClientChannel channel)
        {
            channel.CloseAsync();
        }

        public void Dispose()
        {

        }

        public void ReturnChannel(INiftyClientChannel channel)
        {
            channel.CloseAsync();
        }

        public void ClearChannel(ChannelKey key)
        {

        }
    }
}
