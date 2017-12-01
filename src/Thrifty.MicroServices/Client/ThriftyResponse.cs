using System; 

namespace Thrifty.MicroServices.Client
{
    public class ThriftyResponse : Ribbon.Client.IResponse
    {
        public ThriftyResponse(object result, bool success)
        {
            Payload = result;
            Success = success;
        }
        public object Payload { get; }
        public bool HasPayload => Payload != null;
        public bool Success { get; }
        public Uri RequestedUri => null;
    }
}
