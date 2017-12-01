using System;
using System.Collections.Concurrent;
namespace Thrifty.MicroServices.Ribbon
{
    public class DefaultServerStatusCollector : IServerStatusCollector
    {
        private sealed class DefaultServerStatus : IServerStatus
        {
            private long _lastConnectionFailedTimestamp;
            private readonly Counter _successiveConnectionFailureCounter;
            private readonly Counter _serverFailureCounter;
            private readonly Counter _activeRequestCounter;
            private readonly Counter _openConnectionsCounter;
            private readonly Counter _requestCountCounter;
            private readonly Distribution _responseTimeDistribution;
            private readonly int _connectionFailureThreshold;
            private readonly int _circuitTrippedTimeoutFactor;
            private readonly int _maxCircuitTrippedTimeout;

            public DefaultServerStatus(Server server, int connectionFailureThreshold, int circuitTrippedTimeoutFactor, int maxCircuitTrippedTimeout)
            {
                if (server == null) throw new ArgumentNullException(nameof(server));
                Server = server;
                _successiveConnectionFailureCounter = new Counter();
                _serverFailureCounter = new Counter();
                _activeRequestCounter = new Counter();
                _openConnectionsCounter = new Counter();
                _requestCountCounter = new Counter();
                _responseTimeDistribution = new Distribution();
                _lastConnectionFailedTimestamp = 0;
                _connectionFailureThreshold = connectionFailureThreshold;
                _circuitTrippedTimeoutFactor = circuitTrippedTimeoutFactor;
                _maxCircuitTrippedTimeout = maxCircuitTrippedTimeout;
            }

            public void IncrementActiveRequestsCount() => _activeRequestCounter.Increment();
            public void DecrementActiveRequestsCount() => _activeRequestCounter.Decrement();
            public void ClearSuccessiveConnectionFailureCount() => _successiveConnectionFailureCounter.Value = 0;
            public void IncreaseOpenConntionsCount() => _openConnectionsCounter.Increment();
            public void IncreaseServerFailureCount() => _serverFailureCounter.Increment();
            public void IncrementRequestCount() => _requestCountCounter.Increment();
            public void IncreaseSuccessiveConnectionFailureCount()
            {
                _lastConnectionFailedTimestamp = DateTime.Now.Ticks;
                _successiveConnectionFailureCounter.Increment();
            }
            public Server Server { get; }
            public long ActiveRequestsCount => _activeRequestCounter.Value;
            public long FailureCount => _serverFailureCounter.Value;
            public long SuccessiveConnectionFailureCount => _successiveConnectionFailureCounter.Value;
            public long OpenConnectionsCount => _openConnectionsCounter.Value;
            public long RequestCount => _requestCountCounter.Value;
            public double MaximumResponseTime => _responseTimeDistribution.Maximum;
            public double MinimumResponseTime => _responseTimeDistribution.Minimum;
            public double ResponseTimeAverage => _responseTimeDistribution.Average;
            public double ResponseTimeStdDev => _responseTimeDistribution.StdDev;
            public void RecordResponseTime(long time) => _responseTimeDistribution.NoteValue(time);
            public bool IsCircuitBreakerTripped(DateTime time)
            {
                var blackOutPeriod = GetCircuitBreakerBlackoutPeriod();
                if (blackOutPeriod <= 0) blackOutPeriod = 0;
                var circuitBreakerTimeout = _lastConnectionFailedTimestamp + blackOutPeriod;
                if (circuitBreakerTimeout <= 0) return false;
                return circuitBreakerTimeout > time.Ticks;
            }
            private long GetCircuitBreakerBlackoutPeriod()
            {
                var failureCount = _successiveConnectionFailureCounter.Value;
                var threshold = _connectionFailureThreshold;
                if (failureCount < threshold) return 0;
                var diff = (int)((failureCount - threshold) > 16 ? 16 : (failureCount - threshold));
                var blackOutSeconds = (1 << diff) * _circuitTrippedTimeoutFactor;
                if (blackOutSeconds > _maxCircuitTrippedTimeout) blackOutSeconds = _maxCircuitTrippedTimeout;
                return blackOutSeconds * 1000L;
            }
        }

        private readonly int _connectionFailureThreshold;
        private readonly int _circuitTrippedTimeoutFactor;
        private readonly int _maxCircuitTrippedTimeout;
        public DefaultServerStatusCollector(int connectionFailureThreshold = 10, int circuitTrippedTimeoutFactor = 100, int maxCircuitTrippedTimeout = 90)
        {
            _connectionFailureThreshold = connectionFailureThreshold;
            _circuitTrippedTimeoutFactor = circuitTrippedTimeoutFactor;
            _maxCircuitTrippedTimeout = maxCircuitTrippedTimeout;
        }
        private readonly ConcurrentDictionary<Server, DefaultServerStatus> _status = new ConcurrentDictionary<Server, DefaultServerStatus>();
        private DefaultServerStatus GetStatus(Server server) =>
            _status.GetOrAdd(server, s => new DefaultServerStatus(s, _connectionFailureThreshold, _circuitTrippedTimeoutFactor, _maxCircuitTrippedTimeout));
        public void ClearSuccessiveConnectionFailureCount(Server server) => GetStatus(server).ClearSuccessiveConnectionFailureCount();
        public void IncreaseOpenConntionsCount(Server server) => GetStatus(server).IncreaseOpenConntionsCount();
        public void IncreaseServerFailureCount(Server server) => GetStatus(server).IncreaseServerFailureCount();
        public void IncreaseSuccessiveConnectionFailureCount(Server server) => GetStatus(server).IncreaseSuccessiveConnectionFailureCount();
        public void RecordResponseTime(long time, Server server) => GetStatus(server).RecordResponseTime(time);
        public IServerStatus ServerStatus(Server server) => GetStatus(server);
        public void IncrementActiveRequestsCount(Server server) => GetStatus(server).IncrementActiveRequestsCount();
        public void DecrementActiveRequestsCount(Server server) => GetStatus(server).DecrementActiveRequestsCount();
        public void IncrementRequestCount(Server server) => GetStatus(server).IncrementRequestCount();
    }
}
