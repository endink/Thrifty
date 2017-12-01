namespace Ribbon.Test.Just4Fun
{
    /// <summary>
    /// 远程方法调用器
    /// </summary>
    public interface IRemoteProcedureCaller
    {
        /// <summary>
        /// 在某个服务器上调用远程过程
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="procedure">远程过程</param>
        /// <param name="parameters">调用的过程参数</param>
        /// <returns>远程过程返回值</returns>
        object Call(IServer server, IRemoteProcedure procedure, IProcedureParameter[] parameters);
    }
}
