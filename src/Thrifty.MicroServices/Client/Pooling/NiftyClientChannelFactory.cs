using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chopin.Pooling;
using Thrifty.Nifty.Client;
using Thrifty.Services;
using Chopin.Pooling.Impl;

namespace Thrifty.MicroServices.Client.Pooling
{
    public class NiftyClientChannelFactory : BaseKeyedPooledObjectFactory<ChannelKey, INiftyClientChannel>
    {
        private ThriftyClientOptions _options;
        private Func<ChannelKey, INiftyClientChannel> _createChannel;

        public NiftyClientChannelFactory(ThriftClientManager thriftClientManager, ThriftyClientOptions options)
        {
            Guard.ArgumentNotNull(options, nameof(options));
            Guard.ArgumentNotNull(thriftClientManager, nameof(thriftClientManager));

            _createChannel = key =>
            {
                var connector = new FramedClientConnector(key.IpEndPoint.Address.ToString(), key.IpEndPoint.Port,
                    _options.LoggerFactory);

                var channel = thriftClientManager.CreateChannelAsync(connector, TimeSpan.FromMilliseconds(key.ConnectionTimeout),
                    TimeSpan.FromMilliseconds(key.ReceiveTimeout), TimeSpan.FromMilliseconds(key.ReadTimeout),
                    TimeSpan.FromMilliseconds(key.WriteTimeout), _options.MaxFrameSize, key.SslConfig, _options.SocketProxy);

                return channel.GetAwaiter().GetResult();
            };
            _options = options;
        }

        public override void DestroyObject(ChannelKey key, IPooledObject<INiftyClientChannel> p)
        {
            base.DestroyObject(key, p);
            p.Object.CloseAsync();
        }

        public override bool ValidateObject(ChannelKey key, IPooledObject<INiftyClientChannel> p)
        {
            base.ValidateObject(key, p);
            return p.Object.NettyChannel.Active && !p.Object.HasError;
        }

        public override INiftyClientChannel Create(ChannelKey key)
        {
            return _createChannel.Invoke(key);
        }

        public override IPooledObject<INiftyClientChannel> Wrap(INiftyClientChannel value)
        {
            return new DefaultPooledObject<INiftyClientChannel>(value);
        }
    }
}
