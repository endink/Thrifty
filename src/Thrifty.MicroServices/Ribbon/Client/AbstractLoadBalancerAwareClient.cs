using System;

namespace Thrifty.MicroServices.Ribbon.Client
{
    public abstract class AbstractLoadBalancerAwareClient<TReq, TRes> : IClient<TReq, TRes>
        where TRes : IResponse
        where TReq : IClientRequest
    { 
        protected abstract RequestSpecificRetryHandler GetRetryHandler(TReq request);
        public TRes Execute(TReq request)
        {
            throw new NotImplementedException();
        }
    }
}
