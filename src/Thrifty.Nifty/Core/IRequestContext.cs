using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Protocol;

namespace Thrifty.Nifty.Core
{
    public interface IRequestContext
    {
        Guid Id { get; }

        TProtocol OutputProtocol { get; }

        TProtocol InputProtocol { get; }

        IConnectionContext ConnectionContext { get; }

        void SetContextData(String key, Object val);

        Object GetContextData(String key);

        void ClearContextData(String key);

        IEnumerable<KeyValuePair<String, Object>> ContextData { get; }
    }

    public static class RequestContexts
    {
        private static AsyncLocal<IRequestContext> _threadLocalContext = new AsyncLocal<IRequestContext>();

        ///<summary>
        /// Gets the thread-local <see cref="NiftyRequestContext"/> for the Thrift request that is being processed
        /// on the current thread.
        ///
        ///@return The <see cref="NiftyRequestContext"/> of the current request
        ///</summary>
        public static IRequestContext GetCurrentContext()
        {
            IRequestContext currentContext = _threadLocalContext.Value;
            return currentContext;
        }

        ///<summary>
        ///Sets the thread-local context for the currently running request.
        ///
        ///This is normally called only by the server, but it can also be useful to call when
        ///dispatching to another thread (e.g. a thread in an ExecutorService) if the code that will
        ///run on that thread might also be interested in the <see cref="IRequestContext"/>
        ///</summary>
        public static void SetCurrentContext(IRequestContext requestContext)
        {
            _threadLocalContext.Value = requestContext;
        }

        /**
         * Gets the thread-local context for the currently running request
         *
         * This is normally called only by the server, but it can also be useful to call when
         * cleaning up a context
         */
        public static void ClearCurrentContext()
        {
            IRequestContext old = _threadLocalContext.Value;
            (old as IDisposable)?.Dispose();
            _threadLocalContext.Value = null;
        }
    }

}
