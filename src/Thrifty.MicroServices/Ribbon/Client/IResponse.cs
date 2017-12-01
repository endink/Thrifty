using System; 

namespace Thrifty.MicroServices.Ribbon.Client
{
    public interface IResponse
    {
        object Payload { get; }
        bool HasPayload { get; }
        bool Success { get; }
        Uri RequestedUri { get; }
    }
}
