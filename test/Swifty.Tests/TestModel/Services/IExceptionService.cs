using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Thrift;

namespace Thrifty.Tests.TestModel.Services
{
    [ThriftService]
    public interface IExceptionService : IDisposable
    {
        [ThriftMethod]
        void ThrowArgumentException();

        [ThriftMethod]
        void ThrowTException();
    }

    public class ExceptionService : IExceptionService
    {
        public const string ExceptionMessage = "abcdefg";
        public void Dispose()
        {
        }

        [DebuggerStepThrough]
        public void ThrowArgumentException()
        {
            throw new ArgumentException(ExceptionMessage);
        }

        [DebuggerStepThrough]
        public void ThrowTException()
        {
            throw new TException(ExceptionMessage);
        }
    }
}
