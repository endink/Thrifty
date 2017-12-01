using System;
using System.Reflection;

namespace Ribbon.Test.Just4Fun
{
    /// <summary>
    /// 远程过程
    /// </summary>
    public interface IRemoteProcedure
    {
        /// <summary>
        /// 版本
        /// </summary>
        string Version { get; }
        /// <summary>
        /// 调用的接口
        /// </summary>
        Type Interface { get; }
        /// <summary>
        /// 调用的方法
        /// </summary>
        MethodInfo Method { get; }
    }
}
