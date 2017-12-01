using System;

namespace Thrifty.MicroServices.Ribbon
{
    /// <summary>
    /// 服务状态
    /// </summary>
    public interface IServerStatus
    {
        /// <summary>
        /// 所属的服务器
        /// </summary>
        Server Server { get; }
        /// <summary>
        /// 失败次数
        /// </summary>
        long FailureCount { get; }
        /// <summary>
        /// 打开的连接次数
        /// </summary>
        long OpenConnectionsCount { get; }
        /// <summary>
        /// 激活的请求数
        /// </summary>
        long ActiveRequestsCount { get; }
        /// <summary>
        /// 请求数
        /// </summary>
        long RequestCount { get; }
        /// <summary>
        /// 连续失败次数
        /// </summary>
        long SuccessiveConnectionFailureCount { get; }
        /// <summary>
        /// 响应时间的平均值
        /// </summary>
        double ResponseTimeAverage { get; }
        /// <summary>
        /// 响应时间的偏差
        /// </summary>
        double ResponseTimeStdDev { get; }
        /// <summary>
        /// 最大响应时间
        /// </summary>
        double MaximumResponseTime { get; }
        /// <summary>
        /// 最小响应时间
        /// </summary>
        double MinimumResponseTime { get; }
        /// <summary>
        /// 服务器是否暂时被中断
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns>是否暂时被中断</returns>
        bool IsCircuitBreakerTripped(DateTime time);
    }
}
