using System;
using System.Linq;
using System.Threading;
using Thrifty.MicroServices.Client.Pooling;
using Thrifty.MicroServices.Commons;

namespace Thrifty.MicroServices.Ribbon
{
    public class DefaultServerWeightAccumulater : Disposable, IServerWeightAccumulater
    {
        public ILoadBalancer LoadBalancer { get; set; }
        private readonly IServerStatusCollector _collector;
        private readonly HashedWheelEvictionTimer _timeoutTimer;
        private double[] _weights;
        private readonly ReaderWriterLockSlim _weightsLock;
        private volatile int _accumulatingInProgress = -1;
        public DefaultServerWeightAccumulater(IServerStatusCollector collector, int serverWeightTaskTimerInterval = 30 * 1000)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _weightsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _timeoutTimer = new HashedWheelEvictionTimer(Utils.Timer);
            _timeoutTimer.Schedule(Accumulate, TimeSpan.FromMilliseconds(serverWeightTaskTimerInterval));
            Accumulate();
        }

        private void SetWeights(double[] weights)
        {
            _weightsLock.EnterWriteLock();
            _weights = weights;
            _weightsLock.ExitWriteLock();
        }
        private void Accumulate()
        {
            var accumulater = this;
            var loadBalancer = accumulater.LoadBalancer;
            if (loadBalancer == null) return;
            if (Interlocked.CompareExchange(ref accumulater._accumulatingInProgress, 1, 0) == 1) return;
            var collector = accumulater._collector;
            try
            {
                var statuses = loadBalancer.ReachableServers().Select(server => collector.ServerStatus(server)).ToArray();
                var totalResponseTime = statuses.Sum(status => status.ResponseTimeAverage);
                var length = statuses.Length;
                var weights = new double[length];
                var weightSoFar = 0.0d;
                for (var i = 0; i < length; i++)
                {
                    var weight = totalResponseTime - statuses[i].ResponseTimeAverage;
                    weightSoFar += weight;
                    weights[i] = weightSoFar;
                }
                accumulater.SetWeights(weights);
            }
            finally
            {
                Interlocked.Exchange(ref accumulater._accumulatingInProgress, 0);
            }
        }
        public double[] AccumulatedWeights
        {
            get
            {
                _weightsLock.EnterReadLock();
                var weights = _weights;
                _weightsLock.ExitReadLock();
                if (weights == null) return new double[0];
                var length = weights.Length;
                var tmp = new double[length];
                Array.Copy(weights, tmp, length);
                return tmp;
            }
        }
        protected override void DisposeManagedResource()
        {
            _timeoutTimer.Dispose();
        }
    }
}
