using Thrifty.Tests.Services.Codecs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.TestModel.Services
{
    [ThriftService]
    public interface IAsyncService : IDisposable
    {
        [ThriftMethod]
        void VerifyConnectionState();

        [ThriftMethod(OneWay = true)]
        Task OneWayMethod();

        [ThriftMethod]
        Task ExceptionMethod();

        [ThriftMethod]
        Task<SimpleStruct> TwoWayMethod(SimpleStruct structObjec);
    }

    public class AsyncService : IAsyncService
    {
        public const string ExceptionMessage = "async service exception";
        public void Dispose()
        {
        }

       // [DebuggerStepThrough]
        public Task ExceptionMethod()
        {
            throw new Exception(ExceptionMessage);
        }

        public Task OneWayMethod()
        {
            return Task.FromResult(0);
        }

        public Task<SimpleStruct> TwoWayMethod(SimpleStruct structObjec)
        {
            return Task.FromResult(structObjec);
        }

        public void VerifyConnectionState()
        {
            
        }
    }
}
