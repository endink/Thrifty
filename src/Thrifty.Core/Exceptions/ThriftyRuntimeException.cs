using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    ///<summary>
    ///Runtime equivalent of TException.  If a swift client receives a TException
    ///for a method that doesn't declare TException to be thrown, the underlying
    ///exception is wrapped in this class and rethrown.
    ///</summary>
    public class ThriftyRuntimeException : ThriftyException
    {
        /// <summary>
        /// Initializes a new instance of the Exception class.
        /// </summary>
        public ThriftyRuntimeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ThriftyRuntimeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message.
        /// </summary>
		/// <param name="messageFormat">The exception message format.</param>
		/// <param name="args">The exception message arguments.</param>
        public ThriftyRuntimeException(string messageFormat, params object[] args)
            : base(string.Format(messageFormat, args))
        {
        }

        /// <summary>
        /// Initializes a new instance of the Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ThriftyRuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
