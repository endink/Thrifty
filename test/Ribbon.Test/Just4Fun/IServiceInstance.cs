namespace Ribbon.Test.Just4Fun
{
    /// <summary>
    /// 服务实例
    /// </summary>
    public interface IServiceInstance
    {
        /// <summary>
        /// 服务器
        /// </summary>
        IServer Server { get; }
        /// <summary>
        /// 全部可用的远程过程
        /// </summary>
        IRemoteProcedure[] Procedures { get; }
    }
}
