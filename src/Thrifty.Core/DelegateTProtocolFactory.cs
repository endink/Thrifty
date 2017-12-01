using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Transport;

namespace Thrifty
{
    public class DelegateTProtocolFactory : TProtocolFactory
    {
        private Func<TTransport, TProtocol> _func = null;

        public DelegateTProtocolFactory(Func<TTransport, TProtocol> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException($"DelegateTProtocolFactory 构造函数 {nameof(func)} 参数不能为空。");
            }
            _func = func;
        }

        public TProtocol GetProtocol(TTransport trans)
        {
            return _func(trans);
        }
    }
}
