using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    public class ThriftyTransportException : ThriftyRuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the Exception class.
        /// </summary>
        public ThriftyTransportException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="type"></param>
        public ThriftyTransportException(string message, ExceptionType type = ExceptionType.Unknown)
            : base(message)
        {
            this.ErrorType = type;
        }
        
        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        /// <param name="type"></param>
        public ThriftyTransportException(string message, Exception innerException, ExceptionType type)
            : base(message, innerException)
        {
            this.ErrorType = type;
        }

        public ExceptionType ErrorType { get; }

        public enum ExceptionType
        {
            Unknown,
            NotOpen,
            AlreadyOpen,
            TimedOut,
            EndOfFile,
            Interrupted
        }
    }
}
