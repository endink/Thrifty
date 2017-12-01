using System;

namespace Thrifty.MicroServices.Ribbon
{
    public class DefaultRetryHandler : IRetryHandler
    {
        private readonly int _retrySameServer;
        private readonly int _retryNextServer;
        private readonly bool _retryEnabled;
        private readonly Predicate<Exception> _isCircuitTrippingException;
        private readonly Func<Exception, bool, bool> _isRetriableException;

        public DefaultRetryHandler(int retrySameServer = 0,
            int retryNextServer = 0,
            bool retryEnabled = false,
            Predicate<Exception> isCircuitTrippingException = null,
            Func<Exception, bool, bool> isRetriableException = null)
        {
            _retrySameServer = retrySameServer;
            _retryNextServer = retryNextServer;
            _retryEnabled = retryEnabled;
            _isCircuitTrippingException = isCircuitTrippingException ?? (e => true);
            _isRetriableException = isRetriableException ?? ((e, s) => false);
        }

        public int MaxRetriesOnNextServer => _retryEnabled ? _retryNextServer : 0;
        public int MaxRetriesOnSameServer => _retryEnabled ? _retrySameServer : 0;
        public bool IsCircuitTrippingException(Exception exception) => _isCircuitTrippingException(exception);
        public bool IsRetriableException(Exception exception, bool sameServer) => _isRetriableException(exception, sameServer);
    }
}
