using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    public class ThriftServerConfig
    {
        private const int DEFAULT_WORKER_THREAD_COUNT = 200;
        private const int DEFAULT_PER_CONNECTION_QUEUED_RESPONSE_LIMIT = 16;

        private long _maxFrameSizeBytes;
        private int _port;
        private int _acceptBacklog;
        private int _acceptorThreadCount;
        private int? _ioThreadCount;
        private int _workerThreadCount;

        public ThriftServerConfig()
        {
            _maxFrameSizeBytes = 1024 * 1024 * 64;
            _acceptBacklog = 1024;
            this.BindingAddress = null;
            _acceptorThreadCount = 1;
            _ioThreadCount = null;
            _workerThreadCount = Environment.ProcessorCount * 2;
        }

        /// <summary>
        ///The default maximum allowable size for a single incoming thrift request or outgoing thrift
        ///response.A server can configure the actual maximum to be much higher(up to 0x3FFFFFFF or
        ///almost 1 GB). The default max could also be safely bumped up, but 64MB is chosen simply
        ///because it seems reasonable that if you are sending requests or responses larger than
        ///that, it should be a conscious decision(something you must manually configure).
        /// </summary>
        public long MaxFrameSizeBytes
        {
            get { return _maxFrameSizeBytes; }
            set
            {
                //‭17179869183‬
                if (_maxFrameSizeBytes > 0x3FFFFFFF)
                {
                    throw new ArgumentException($"{nameof(MaxFrameSizeBytes)} 不能超过 {0x3FFFFFFF} bytes （大约 1GB）");
                }
                if (_maxFrameSizeBytes != value)
                {
                    _maxFrameSizeBytes = value;
                }
            }
        }

        public int Port
        {
            get { return _port; }
            set
            {
                Guard.ArgumentCondition((value > 0 && value <= 65535), $"{nameof(Port)} 表示端口号，必须在 1~65535 之间");
                _port = value;
            }
        }

        /// <summary>
        /// 获取服务器绑定的网络地址（机器名或 IP地址），为空表示 IPAddress.Any.
        /// </summary>
        public string BindingAddress { get; set; }

        /// <summary>
        /// 获取或设置 Netty BOSS 线程池的线程数。
        /// </summary>
        public int AcceptorThreadCount
        {
            get { return _acceptorThreadCount; }
            set
            {
                Guard.ArgumentCondition(value > 0, $"{nameof(AcceptorThreadCount)} 必须大于 0。");
                _acceptorThreadCount = value;
            }
        }

        /// <summary>
        /// TCP accept Backlog 参数, 默认为 1024。
        /// </summary>
        public int AcceptBacklog
        {
            get { return _acceptBacklog; }
            set
            {
                Guard.ArgumentCondition(value > 0, $"{nameof(AcceptBacklog)} 必须大于 0。");
                _acceptBacklog = value;
            }
        }

        /// <summary>
        /// 获取或设置 IO 线程组（Netty Wroker Group）的线程数。
        /// </summary>
        public int? IOThreadCount
        {
            get { return _ioThreadCount; }
            set
            {
                if (value.HasValue)
                {
                    Guard.ArgumentCondition(value.Value > 0, $"{nameof(IOThreadCount)} 必须大于 0。");
                }
                _ioThreadCount = value;
            }
        }

        /// <summary>
        /// 获取或设置工作线程组（Wrok Group）的线程数（默认为CPU核心数 * 2）。
        /// </summary>
        public int WorkerThreadCount
        {
            get { return _workerThreadCount; }
            set
            {
                Guard.ArgumentCondition(value > 0, $"{nameof(WorkerThreadCount)} 必须大于 0。");
                _workerThreadCount = value;
            }
        }

        /// <summary>
        /// 获取或设置服务器空闲超时时间，默认为 1 分钟。
        /// </summary>
        public TimeSpan IdleConnectionTimeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// 服务器接受请求后和服务器从请求队列中拉取请求进行处理之间的超时时间。 如果超过该时间，服务器自动关闭客户端连接。
        /// 该值通常在服务器繁忙时候可以阻止大量的客户端排队，默认为 5 秒。
        /// </summary>
        public TimeSpan QueueTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 服务器收到客户端请求到请求处理完成之间的超时时间，默认为 5 秒。
        /// 如果在请求到达队列前面并开始处理之前超时过期,服务器将丢弃请求而不是处理它。
        /// 如果请求已开始处理, 服务器将立即发送错误, 并丢弃请求处理的结果。
        /// </summary>
        public TimeSpan TaskExpirationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 获取或设置服务器链接数限制，默认不限，即 null。
        /// </summary>
        public int? ConnectionLimit { get; set; }

        /// <summary>
        /// 获取或设置 Thrift （序列化）协议，默认为 binary。
        /// </summary>
        public String ProtocolName { get; set; } = "binary";

        /// <summary>
        /// 获取或设置传输协议，默认为 framed。
        /// </summary>
        public String TransportName { get; set; } = "framed";

        /// <summary>
        /// 获取或设置连接前每个连接可能累积的最大响应数，开始阻塞读取（以避免生成无限的排队响应）。
        /// 每当服务端处理的请求和超出响应顺序时, 服务端就会限制响应直到预期序号的请求到来，默认为 16。
        /// </summary>
        public int MaxQueuedResponsesPerConnection { get; set; }
    }
}
