using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Thrifty.Tests
{
    public static class AssertEx
    {
        public static void Equals<K, V>(IDictionary<K, V> d1, IDictionary<K, V> d2)
        {
            Assert.NotNull(d1);
            Assert.NotNull(d2);
            Assert.Equal(d1.Count, d2.Count);
            foreach (var kp in d1)
            {
                Assert.True(d2.ContainsKey(kp.Key), $"expected key was not found: {kp.Key}");
                Assert.Equal(kp.Value, d2[kp.Key]);
            }
        }

        public static void Equals<T>(IEnumerable<T> d1, IEnumerable<T> d2)
        {
            Assert.NotNull(d1);
            Assert.NotNull(d2);
            Assert.Equal(d1.Count(), d2.Count());
            int index = 0;
            var d2Array = d2.ToArray();
            foreach (var v in d1)
            {
                Assert.Equal(v, d2Array.ElementAt(index));
                index++;
            }
        }
    }
}
