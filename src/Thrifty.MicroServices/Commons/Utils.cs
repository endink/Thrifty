using DotNetty.Common.Utilities;
using System;
namespace Thrifty.MicroServices.Commons
{
    internal interface IIntervalAction
    {
        void Start();
        void Stop();
    }
    internal class Utils
    { 
        private class HashedWheelTimerWrapper : IDisposable
        {
            public static readonly HashedWheelTimerWrapper Instance = new HashedWheelTimerWrapper();
            public HashedWheelTimer Timer { get; } =
                new HashedWheelTimer(TimeSpan.FromMilliseconds(500), 2 * 60, -1);// 一分钟的时间轮，每个刻度 500 毫秒。
            public void Dispose()
            {
                Timer.StopAsync().GetAwaiter().GetResult();
            }
        }
        public static HashedWheelTimer Timer => HashedWheelTimerWrapper.Instance.Timer;
    }
}
