using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Thrifty.Nifty.Ssl;

namespace Thrifty.Nifty.Client
{
    public class ClientSslConfig
    {
        private IFileProvider _fileProvider;
        private String _certFile;
        private Lazy<X509Certificate2> _cert;

        public string CertFile
        {
            get { return _certFile; }
            set
            {
                if (_certFile != value)
                {
                    _certFile = value;
                    _cert?.Value?.Dispose();
                    _cert = null;
                    if (_certFile != null)
                    {
                        _cert = new Lazy<X509Certificate2>(() =>
                        {
                            var bytes = this.FileProvider.ReadAllBytes(_certFile);
                            return new X509Certificate2(bytes);
                        }, true);
                    }
                }
            }
        }

        public IFileProvider FileProvider
        {
            get { return _fileProvider ?? (_fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory())); }
            set { _fileProvider = value; }
        }

        internal bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (_cert != null)
            {
                X509Certificate2 ca = _cert.Value;

                using (X509Chain chain2 = new X509Chain())
                {
                    chain2.ChainPolicy.ExtraStore.Add(ca);
                    chain2.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    chain2.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain2.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;
                    
                    X509Certificate2 cert = new X509Certificate2(certificate.Export(X509ContentType.Cert));

                    //AllowUnknownCertificateAuthority 无法解决 UntrustedRoot 问题。
                    //https://stackoverflow.com/questions/27307322/verify-server-certificate-against-self-signed-certificate-authority?rq=1
                    if (chain2.Build(cert))
                    {
                        var chainThumbprint = chain2.ChainElements[chain2.ChainElements.Count - 1].Certificate.Thumbprint;
                        return chainThumbprint  == ca.Thumbprint; // success?
                    }
                    return false;
                    //return (chain2.ChainStatus.Length == 0 || chain2.ChainStatus[0].Status == X509ChainStatusFlags.NoError);
                }
            }

            return (chain.ChainStatus.Length == 0 || chain.ChainStatus[0].Status == X509ChainStatusFlags.NoError);
        }
    }
}
