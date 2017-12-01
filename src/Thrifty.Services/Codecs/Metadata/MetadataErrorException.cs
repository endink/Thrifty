using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class MetadataErrorException : ThriftyException
    {
        private HashSet<Exception> _innerExceptions = null;

        public MetadataErrorException(String message)
            : this(message, null)
        {
            
        }

        public MetadataErrorException(string message, Exception cause)
            : base(message, cause)
        {
            this._innerExceptions = new HashSet<Exception>();
            if (cause != null)
            {
                this.AddException(cause);
            }
        }
        
        public IEnumerable<Exception> Exceptions { get { return _innerExceptions; } }

        public void AddException(Exception ex)
        {
            if (ex != null)
            {
                this._innerExceptions.Add(ex);
            }
        }

        public override string Message
        {
            get
            {
                StringBuilder builder = new StringBuilder($"{base.Message}.");
                if (_innerExceptions.Count > 0)
                {
                    builder.AppendLine($"exception {this._innerExceptions.Count}:");
                    foreach (var e in this.Exceptions)
                    {
                        builder.AppendLine($"[{e.GetType().Name}] {e.Message}");
                    }
                }
                return builder.ToString();
            }
        }
    }
}
