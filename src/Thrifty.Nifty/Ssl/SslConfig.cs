using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Nifty.Ssl
{
     
    public class SslConfig
    {
        private IFileProvider _fileProvider;
        
        public string CertFile { get; set; }
        
        public string CertPassword { get; set; }

        public IFileProvider CertFileProvider
        {
            get { return _fileProvider ?? (_fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory())); }
            set { _fileProvider = value; }
        }
        
    }
}
