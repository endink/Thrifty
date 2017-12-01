using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Thrifty.MicroServices.Client.Pooling;
using Thrifty.MicroServices.Commons;

namespace Thrifty.MicroServices.Ribbon
{
    public delegate void ServersChangedEventHander(object sender, Server[] changedServers);

    public class BaseLoadBalancer : Disposable, ILoadBalancer
    {
        private readonly string _name;
        private readonly IRule _rule;
        private readonly IPing _ping;
        private readonly IPingStrategy _pingStrategy;
        private volatile int _pingInProgress = -1;
        private readonly ReaderWriterLockSlim _allServerLock;
        private readonly ReaderWriterLockSlim _upServerLock;
        protected ConcurrentBag<Server> AllServer = new ConcurrentBag<Server>();
        protected ConcurrentBag<Server> UpServer = new ConcurrentBag<Server>();
        private readonly Counter _counter;
        private readonly ILogger _logger;
        private bool _handing = false;

        private readonly HashedWheelEvictionTimer _timeoutTimer;

        public BaseLoadBalancer(int pingInterval, string name, IRule rule, IPing ping, IPingStrategy pingStrategy, ILoggerFactory factory)
        {
            _name = name;
            _rule = rule;
            _ping = ping;
            if (_ping == null) throw new ArgumentNullException(nameof(ping));
            _logger = factory?.CreateLogger(typeof(BaseLoadBalancer)) ?? NullLogger.Instance;
            _pingStrategy = pingStrategy ?? new SerialPingStrategy();
            _allServerLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _upServerLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _counter = new Counter();
            _timeoutTimer = new HashedWheelEvictionTimer(Utils.Timer);
            _timeoutTimer.Schedule(this.RunPing, TimeSpan.FromMilliseconds(pingInterval));
            RunPing();
        }


        public event ServersChangedEventHander ServersChanged;

        private void RunPing()
        {
            try
            {
                if (_ping == null) return;
                if (_handing) return;
                _handing = true;

                _allServerLock.EnterReadLock();
                var servers = AllServer.Where(x => x.ReadyToServe).ToArray();
                _allServerLock.ExitReadLock();

                var result = _pingStrategy.PingServers(_ping, servers);
                if (Interlocked.CompareExchange(ref _pingInProgress, 1, 0) == 1) return;

                _logger.LogDebug("开始执行ping操作");
                var changedServers = new List<Server>();
                var upServers = new List<Server>();
                try
                {
                    foreach (var item in result)
                    {
                        var server = item.Server;
                        var isAlive = item.IsAlive;
                        if (server.IsAlive != isAlive)
                        {
                            changedServers.Add(item.Server);
                            _logger.LogDebug($"LoadBalancer [{_name}]:  Server [{server}] status changed to {(isAlive ? "ALIVE" : "DEAD")}");
                        }
                        if (isAlive)
                        {
                            server.Unzombify();
                            upServers.Add(server);
                        }
                    }
                    _upServerLock.EnterWriteLock();
                    UpServer = new ConcurrentBag<Server>(upServers);
                    _upServerLock.ExitWriteLock();
                    ServersChanged?.Invoke(this, changedServers.ToArray());
                }
                finally
                {
                    if (_upServerLock.IsWriteLockHeld) _upServerLock.ExitWriteLock();
                    Interlocked.Exchange(ref _pingInProgress, 0);
                }
            }
            finally
            {
                if (_allServerLock.IsReadLockHeld) _allServerLock.ExitReadLock();
                _handing = false;
            }
        }

        protected void ResetServers(IList<Server> servers)
        {
            var rightServers = new List<Server>();
            foreach (var server in from server in servers where server != null select server)
            {
                server.Online();
                rightServers.Add(server);
                _logger.LogDebug($"LoadBalancer [{_name}]:  addServer [{server}]");
            }
            try
            {
                _allServerLock.EnterReadLock();
                var allServer = AllServer.ToList();
                _allServerLock.ExitReadLock();
                var newServers = (from server in rightServers where !allServer.Contains(server) select server).ToArray();
                _allServerLock.EnterWriteLock();
                AllServer = new ConcurrentBag<Server>(newServers.Union(allServer));
                _allServerLock.ExitWriteLock();
                if (newServers.Length != 0)
                {
                    Interlocked.Exchange(ref _pingInProgress, 0);
                    RunPing();
                    ServersChanged?.Invoke(this, newServers);
                }
            }
            finally
            {
                if (_allServerLock.IsReadLockHeld) _allServerLock.ExitReadLock();
                if (_allServerLock.IsWriteLockHeld) _allServerLock.ExitWriteLock();
            }
        }

        public void AddServers(IList<Server> servers)
        {
            if (servers == null || servers.Count == 0) return;
            ResetServers(servers);
        }

        public IList<Server> AllServers()
        {
            _allServerLock.EnterReadLock();
            var servers = AllServer.ToList();
            _allServerLock.ExitReadLock();
            return servers.AsReadOnly();
        }

        public Server Choose()
        {
            while (true)
            {
                if (_pingInProgress == 1)
                {
                    Thread.Sleep(5 * 1000);
                }
                else
                {
                    _counter.Increment();
                    try
                    {
                        return _rule.Choose(this);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError($"LoadBalancer [{_name}]:  Error choosing server for rule {_rule}", exception);
                        return null;
                    }
                }
            }
        }

        public void MarkServerDown(Server server)
        {
            if (server == null || !server.IsAlive || !server.ReadyToServe) return;
            _logger.LogDebug($"LoadBalancer [{_name}]:  markServerDown called on [{server}]");
            try
            {
                _allServerLock.EnterWriteLock();
                server.Zombify();
                server.Offline();
                _allServerLock.ExitReadLock();
                ServersChanged?.Invoke(this, new Server[] { server });
            }
            finally
            {
                if (_allServerLock.IsReadLockHeld)
                    _allServerLock.ExitReadLock();
            }
        }

        public IList<Server> ReachableServers()
        {
            _upServerLock.EnterReadLock();
            var servers = UpServer.ToList();
            _upServerLock.ExitReadLock();
            return servers.AsReadOnly();
        }

        protected override void DisposeManagedResource()
        {
            _timeoutTimer.Dispose();
        }
    }
}
