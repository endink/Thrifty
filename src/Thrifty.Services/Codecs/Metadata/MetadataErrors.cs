using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class MetadataErrors
    {
        private readonly List<MetadataErrorException> _errors;
        private readonly List<MetadataWarningException> _warnings;
        private readonly IMonitor _monitor;
        public static readonly IMonitor NullMonitor = new NullMonitor();

        public interface IMonitor
        {
            void onError(MetadataErrorException errorMessage);

            void onWarning(MetadataWarningException warningMessage);
        }

        public MetadataErrors(IMonitor monitor = null)
        {
            this._monitor = monitor ?? MetadataErrors.NullMonitor;
            this._errors = new List<MetadataErrorException>();
            this._warnings = new List<MetadataWarningException>();
        }

        public void ThrowIfHasErrors()
        {
            if (_errors.Any())
            {
                MetadataErrorException exception = new MetadataErrorException(
                        $"Metadata extraction encountered {_errors.Count} errors and {_warnings.Count()} warnings");
                foreach (MetadataErrorException error in _errors)
                {
                    exception.AddException(error);
                }
                foreach (MetadataWarningException warning in _warnings)
                {
                    exception.AddException(warning);
                }
                throw exception;
            }
        }

        public IEnumerable<MetadataErrorException> Errors
        {
            get { return _errors; }
        }

        public void AddError(String errorMessage)
        {
            MetadataErrorException message = new MetadataErrorException(errorMessage);
            _errors.Add(message);
            _monitor.onError(message);
        }

        public void AddError(String message, Exception e)
        {
            MetadataErrorException ex = new MetadataErrorException(message, e);
            _errors.Add(ex);
            _monitor.onError(ex);
        }

        public IEnumerable<MetadataWarningException> Warnings
        {
            get { return _warnings; }
        }

        public void AddWarning(String message)
        {
            MetadataWarningException ex = new MetadataWarningException(message);
            _warnings.Add(ex);
            _monitor.onWarning(ex);
        }

        public void AddWarning(String message, Exception e)
        {
            MetadataWarningException ex = new MetadataWarningException(message, e);
            _warnings.Add(ex);
            _monitor.onWarning(ex);
        }

        public override String ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (MetadataErrorException error in _errors)
            {
                builder.Append(error.Message).Append(Environment.NewLine);
            }
            foreach (MetadataWarningException warning in _warnings)
            {
                builder.Append(warning.Message).Append(Environment.NewLine);
            }
            return builder.ToString();
        }
    }

    internal sealed class NullMonitor : MetadataErrors.IMonitor
    {
        public void onError(MetadataErrorException errorMessage)
        {

        }

        public void onWarning(MetadataWarningException warningMessage)
        {

        }
    }

}
