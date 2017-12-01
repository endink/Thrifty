using System;

namespace Thrifty.MicroServices.Ribbon
{
    public interface IRetryHandler
    {
        /// <summary>
        /// 是否重试的异常
        /// </summary> 
        bool IsRetriableException(Exception exception, bool isSameServer);
        /// <summary>
        /// 是否中断异常
        /// </summary> 
        bool IsCircuitTrippingException(Exception exception);
        /// <summary>
        /// 同server最大retry次数
        /// </summary>
        int MaxRetriesOnSameServer { get; }
        /// <summary>
        /// 新server最大retry次数
        /// </summary>
        int MaxRetriesOnNextServer { get; }
    }
}
