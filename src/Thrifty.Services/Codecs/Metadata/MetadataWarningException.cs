using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class MetadataWarningException : ThriftyException
    {
        public MetadataWarningException(String message)
            : base($"Warning: {message}")
        {
        }

        public MetadataWarningException(string message, Exception cause)
            : base($"Warning: {message}", cause)
        {
        }
    }
}
