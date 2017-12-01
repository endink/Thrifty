
using System;
using System.Collections.Generic;

namespace Thrifty.MicroServices.Ribbon
{
    /// <summary>
    /// 服务器
    /// </summary>
    public class Server : IEquatable<Server>, IEqualityComparer<Server>
    {
        private readonly string _id;
        private readonly string _host;
        private readonly int _port;
        private readonly int _hashCode;
        private volatile bool _isAlive;
        private volatile bool _readyToServer;
        public Server(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("host不能为空", nameof(host));
            if (port < 0 && port > 65535) throw new ArgumentException("", nameof(port));
            _host = host;
            _port = port;
            _id = $"{_host}:{_port}";
            _hashCode = _id.GetHashCode();
        }

        public Server(string hostAndPort)
        {
            if (string.IsNullOrWhiteSpace(hostAndPort))
            {
                throw new ArgumentNullException(nameof(hostAndPort));
            }
            if (hostAndPort.StartsWith("http://"))
            {
                hostAndPort = hostAndPort.Substring("http://".Length);
            }
            if (hostAndPort.StartsWith("https://"))
            {
                hostAndPort = hostAndPort.Substring("https://".Length);
            }
            var index = hostAndPort.IndexOf("/");
            if (index > 0)
            {
                hostAndPort = hostAndPort.Substring(0, index);
            }
            index = hostAndPort.IndexOf(':');
            _host = index > 0 ? hostAndPort.Substring(0, index) : hostAndPort;
            _port = index > 0 ? int.Parse(hostAndPort.Substring(index + 1)) : 80;
            _id = $"{_host}:{_port}";
            _hashCode = _id.GetHashCode();
        }

        /// <summary>
        /// 主机IP
        /// </summary>
        public string Host { get { return _host; } }
        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get { return _port; } }
        /// <summary>
        /// 服务是否存活
        /// </summary>
        public bool IsAlive { get { return _isAlive; } }
        /// <summary>
        /// 僵尸化
        /// </summary>
        internal void Zombify() => _isAlive = false;
        /// <summary>
        /// 取消僵尸化
        /// </summary>
        internal void Unzombify() => _isAlive = true;
        /// <summary>
        /// 节点是否可用
        /// </summary>
        public bool ReadyToServe { get { return _readyToServer; } }
        /// <summary>
        /// 启用服务器
        /// </summary>
        internal void Online() => _readyToServer = true;
        /// <summary>
        /// 禁用服务器
        /// </summary>
        internal void Offline() => _readyToServer = false;

        public override string ToString() => _id;
        public override int GetHashCode() => _hashCode;
        public int GetHashCode(Server obj) => obj?._hashCode ?? 0;
        public override bool Equals(object obj) => Equals(obj as Server);
        public bool Equals(Server other) => other?._id == _id;
        public bool Equals(Server x, Server y) => x?.Equals(y) ?? y?.Equals(x) ?? false;
    }
}
