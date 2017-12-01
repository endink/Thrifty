using System; 

namespace Thrifty.MicroServices.Ribbon.Client
{
    public class DefaultClientRequest : IClientRequest
    {
        public DefaultClientRequest(Uri uri, bool retriable)
        {
            Uri = uri;
            Retriable = retriable;
        }

        public Uri Uri { get; }

        public bool Retriable { get; }

        public IClientRequest ReplaceUri(Uri uri) => new DefaultClientRequest(uri, Retriable);
    }
}
