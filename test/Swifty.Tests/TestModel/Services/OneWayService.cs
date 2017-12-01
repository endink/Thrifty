using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Tests.TestModel.Services
{
    [ThriftService]
    public interface IOneWayService : IDisposable
    {
        [ThriftMethod]
        void VerifyConnectionState();

        [ThriftMethod(OneWay = true)]
        void OneWayMethod();

        [ThriftMethod(OneWay = true)]
        void OneWayThrow();
    }

    public class OneWayService : IOneWayService
    {
        public const string ExceptionMessage = "oneway excecption";
        public void Dispose()
        {
        }

        public void VerifyConnectionState()
        {
            
        }

        [DebuggerStepThrough]
        public void OneWayThrow()
        {
            throw new ArgumentException("oneway excecption");
        }

        public void OneWayMethod()
        {
            
        }
    }
}
