using Thrifty.MicroServices.Ribbon;
using Thrifty.Nifty.Client;
using System;

namespace Thrifty.MicroServices.Client.Pooling
{
    public interface IClientConnectionPool : IDisposable
    {
        INiftyClientChannel BorrowChannel(ChannelKey key);

        void ClearChannel(ChannelKey key);
        
        void ReturnChannel(ChannelKey key,INiftyClientChannel channel);
    }
}