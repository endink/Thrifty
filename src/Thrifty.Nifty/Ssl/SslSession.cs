using DotNetty.Handlers.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Ssl
{
    public class SslSession
    {
        public SslSession(
                TlsSettings settings,
                X509Certificate peerCert)
        {
            this.PeerCert = peerCert;
        }


        public TlsSettings TlsSettings { get; }

        public X509Certificate PeerCert { get; }

    }
}
