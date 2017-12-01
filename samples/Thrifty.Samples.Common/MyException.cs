using System;
using System.Collections.Generic;
using System.Text;
using Thrift;

namespace Thrifty.Samples.Common
{
    [ThriftStruct("Exception")]
    public class MyException : Exception
    {
        [ThriftConstructor]
        public MyException(string message)
        {
            Message = message;
        }
        [ThriftField(1)]
        public override string Message { get; }
    }
}
