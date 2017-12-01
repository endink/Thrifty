namespace Ribbon.Test.Just4Fun
{
    /// <summary>
    /// 服务器实例
    /// </summary>
    public interface IServer
    {
        /// <summary>
        /// 主机地址
        /// </summary>
        string Host { get; }
        /// <summary>
        /// 端口
        /// </summary>
        int Port { get; } 
    }
}
