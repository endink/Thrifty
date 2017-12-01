using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Thrifty.Threading
{
    /// <summary>
    /// 一个院子操作的布尔值。
    /// </summary>
    public sealed class AtomicBoolean
    {
        private int value;

        public AtomicBoolean(bool initialValue = false)
        {
            value = initialValue ? 1 : 0;
        }

        public bool Get()
        {
            return value != 0;
        }

        /// <summary>
        /// 如果 <paramref name="expect"/> 和当前的值相等，使用 <paramref name="update"/> 更新。
        /// 返回一个值，指示是否进行了更新。
        /// </summary>
        /// <param name="expect">预期值。</param>
        /// <param name="update">更新值。</param>
        /// <returns>返回是否进行了更新。</returns>
        public bool CompareAndSet(bool expect, bool update)
        {
            int e = expect ? 1 : 0;
            int u = update ? 1 : 0;
            var oldValue = Interlocked.CompareExchange(ref this.value, u, e);
            return oldValue == e;
        }

        public void Set(bool newValue)
        {
            value = newValue ? 1 : 0;
        }

        /// <summary>
        /// 更新一个值并返回原值。
        /// </summary>
        /// <param name="newValue">要更新的值。</param>
        /// <returns></returns>
        public bool GetAndSet(bool newValue)
        {
            bool prev;
            do
            {
                prev = Get();
            } while (!this.CompareAndSet(prev, newValue));
            return prev;
        }

    }
}
