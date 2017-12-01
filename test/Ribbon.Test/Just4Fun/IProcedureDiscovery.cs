namespace Ribbon.Test.Just4Fun
{
    /// <summary>
    /// 远程过程发现
    /// </summary>
    public interface IProcedureDiscovery
    {
        /// <summary>
        /// 获取一个远程过程对应的全部服务实例
        /// </summary>
        /// <typeparam name="T">远程过程</typeparam>
        /// <returns>服务实例</returns>
        IServer[] GetInstances<T>() where T : IProcedure;
    }
}
