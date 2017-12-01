namespace Thrifty.MicroServices.Ribbon.Client
{
    public interface IClient<in TReq, out TRes>
        where TRes : IResponse
        where TReq : IClientRequest
    {
        TRes Execute(TReq request);
    }
}
