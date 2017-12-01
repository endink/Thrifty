using Thrifty.Nifty.Duplex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Nifty.Duplex
{
    public abstract class TDuplexProtocolFactory
    {
        public abstract TProtocolPair GetProtocolPair(TTransportPair transportPair);

        public TProtocolFactory GetInputProtocolFactory()
        {
            return new DelegateTProtocolFactory(t => 
            GetProtocolPair(TTransportPair.FromSingleTransport(t)).InputProtocol);
        }

        public TProtocolFactory GetOutputProtocolFactory()
        {
            return new DelegateTProtocolFactory(trans => 
            GetProtocolPair(TTransportPair.FromSingleTransport(trans)).OutputProtocol);

        }

        public static TDuplexProtocolFactory FromSingleFactory(TProtocolFactory protocolFactory)
        {
            return new DelegateTDuplexProtocolFactory(transportPair =>
            {
                var inputTrans = protocolFactory.GetProtocol(transportPair.InputTransport);
                var outputTrans = protocolFactory.GetProtocol(transportPair.OutputTransport);
                return TProtocolPair.FromSeparateProtocols(inputTrans, outputTrans);
            });
        }

        public static TDuplexProtocolFactory FromSeparateFactories(TProtocolFactory inputProtocolFactory, TProtocolFactory outputProtocolFactory)
        {
            return new DelegateTDuplexProtocolFactory(transportPair =>
            {
                var inputTrans = inputProtocolFactory.GetProtocol(transportPair.InputTransport);
                var outputTrans = outputProtocolFactory.GetProtocol(transportPair.OutputTransport);
                return TProtocolPair.FromSeparateProtocols(inputTrans, outputTrans);
            });
        }
    }

    public class DelegateTDuplexProtocolFactory : TDuplexProtocolFactory
    {
        private Func<TTransportPair, TProtocolPair> _func = null;
        public DelegateTDuplexProtocolFactory(Func<TTransportPair, TProtocolPair> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException($"{nameof(DelegateTDuplexProtocolFactory)} 构造函数 {nameof(func)} 参数不能为空。");
            }
            _func = func;
        }
        public override TProtocolPair GetProtocolPair(TTransportPair transportPair)
        {
            return _func(transportPair);
        }
    }
}


