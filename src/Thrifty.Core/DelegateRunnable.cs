using DotNetty.Common.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty
{
    public class DelegateRunnable : IRunnable
    {
        readonly Action action;

        public DelegateRunnable(Action action)
        {
            this.action = action;
        }

        public void Run() => this.action();
    }
}
