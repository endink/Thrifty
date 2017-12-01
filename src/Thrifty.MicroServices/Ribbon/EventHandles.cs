using System;

namespace Thrifty.MicroServices.Ribbon
{

    public class NoServerFoundException : ThriftyException
    {
        public NoServerFoundException(string message)
            : base(message) { }
    }
    public class ChooseServerErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        internal ChooseServerErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
    public class ServerReadyEventArgs : EventArgs
    {
        public Server Server { get; }
        internal ServerReadyEventArgs(Server server)
        {
            Server = server;
        }
    }
    public class ExecutingExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; }
        internal ExecutingExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
    public class ExecutingSuccessEventArgs : EventArgs
    {
        public object Result { get; }
        internal ExecutingSuccessEventArgs(object result)
        {
            Result = result;
        }
    }
    public delegate void ExecutionStartEventHandler(object sender);
    public delegate void ChooseServerErrorEventHandler(object sender, ChooseServerErrorEventArgs args);
    public delegate void ServerReadyEventHandler(object sender, ServerReadyEventArgs args);
    public delegate void ExecutingExceptionEventHandler(object sender, ExecutingExceptionEventArgs args);
    public delegate void ExecutionSuccessEventHandler(object sender, ExecutingSuccessEventArgs args);
}
