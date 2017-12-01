using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Ssl
{
    public static class CertificateHelper
    {
        public static X509Certificate2 GetCertificate(SslConfig sslConfig)
        {
            X509Certificate2 tlsCertificate = null;

            if (sslConfig != null)
            {
                if (!String.IsNullOrWhiteSpace(sslConfig.CertFile))
                {
                    var fileProvider = sslConfig.CertFileProvider;
                    var filePath = sslConfig.CertFile;
                    byte[] content = ReadAllBytes(fileProvider, filePath);
                    if (string.IsNullOrEmpty(sslConfig.CertPassword))
                    {
                        tlsCertificate = new X509Certificate2(content);
                    }
                    else
                    {
                        tlsCertificate = new X509Certificate2(content, sslConfig.CertPassword);
                    }
                }
            }
            return tlsCertificate;
        }

        public static byte[] ReadAllBytes(this IFileProvider fileProvider, string filePath)
        {
            var file = fileProvider.GetFileInfo(filePath);
            if (!file.Exists)
            {
                throw new ThriftyException($@"Certificate file ""{filePath}"" was not found, provider: {fileProvider.GetType().Name}.");
            }
            byte[] content = new byte[file.Length];
            using (Stream fileStream = file.CreateReadStream())
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    long copyIndex = 0;
                    int readBytes = 0;
                    byte[] buffer = new byte[4096];
                    while ((readBytes = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < readBytes; i++)
                        {
                            content[copyIndex + i] = buffer[i];
                        }
                        copyIndex += readBytes;
                    }
                }
            }

            return content;
        }
    }
}
