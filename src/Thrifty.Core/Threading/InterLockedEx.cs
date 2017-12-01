using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Thrifty.Threading
{
    public static class InterLockedEx
    {
        /// <summary>
        /// 和 <see cref="Interlocked.Increment(ref int)"/> 功能相同，区别在于返回原始值。
        /// </summary>
        public static int GetAndIncrement(ref int location)
        {
            return Interlocked.Increment(ref location) - 1;
        }

        /// <summary>
        /// 和 <see cref="Interlocked.Increment(ref long)"/> 功能相同，区别在于返回原始值。
        /// </summary>
        public static long GetAndIncrement(ref long location)
        {
            return Interlocked.Increment(ref location) - 1;
        }

    }
}
