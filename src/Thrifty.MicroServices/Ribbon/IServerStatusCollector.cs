namespace Thrifty.MicroServices.Ribbon
{
    /// <summary>
    /// 服务器状态采集器
    /// </summary>
    public interface IServerStatusCollector
    {
        /// <summary>
        /// 获取服务器的状态
        /// </summary>
        /// <param name="server">服务器</param>
        /// <returns></returns>
        IServerStatus ServerStatus(Server server);
        /// <summary>
        /// 记录服务器的响应时间
        /// </summary>
        /// <param name="time">时间</param>
        /// <param name="server"></param>
        void RecordResponseTime(long time, Server server);
        /// <summary>
        /// 添加服务器的失败次数
        /// </summary>
        /// <param name="server"></param>
        void IncreaseServerFailureCount(Server server);
        /// <summary>
        /// 添加服务器的打开次数
        /// </summary>
        /// <param name="server"></param>
        void IncreaseOpenConntionsCount(Server server);
        /// <summary>
        /// 添加连续失败次数
        /// </summary>
        /// <param name="server"></param>
        void IncreaseSuccessiveConnectionFailureCount(Server server);
        /// <summary>
        /// 添加服务器的请求数
        /// </summary>
        void IncrementRequestCount(Server server);
        /// <summary>
        /// 清除连续失败次数
        /// </summary>
        /// <param name="server"></param>
        void ClearSuccessiveConnectionFailureCount(Server server);
        /// <summary>
        /// 添加服务器的激活请求数
        /// </summary>
        /// <param name="server"></param>
        void IncrementActiveRequestsCount(Server server);
        /// <summary>
        /// 减少服务器的激活请求数
        /// </summary>
        /// <param name="server"></param>
        void DecrementActiveRequestsCount(Server server);
    }
}
