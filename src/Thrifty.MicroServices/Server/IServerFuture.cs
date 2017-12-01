using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Server
{
    /// <summary>
    /// 服务器特征
    /// </summary>
    public interface IServerFuture
    {
        /// <summary>
        /// 服务器是否启动
        /// </summary>
        bool IsStart { get; }

        /// <summary>
        /// 服务器是否关闭
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// 服务器是否出错
        /// </summary>
        bool IsError { get; }

        /// <summary>
        /// 服务器关闭
        /// </summary>
        /// <returns></returns>
        Task<IServerFuture> ShutdownAsync();

        /// <summary>
        /// 服务器启动
        /// </summary>
        /// <returns></returns>
        Task<IServerFuture> StartAsync();

        /// <summary>
        /// 添加启动监听器
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IServerFuture AddStartListener(Action action);

        /// <summary>
        /// 添加关闭监听器
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IServerFuture AddShutdownListener(Action action);
    }
}