using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Thrifty.Benchmark
{
    public class LogTester3 : LogTester
    {
        protected override int Port => 6666;

        protected override bool EnableConnectionPool => true;

        protected override string EurekaAddress => "http://localhost:8761/eureka/";

        [Benchmark(Description = "LogCase.GetMessages (Eureka)")]
        public override void RunGetMessages()
        {
            base.RunGetMessages();
        }


        [Benchmark(Description = "LogCase.Log (Eureka)")]
        public override void RunLog()
        {
            base.RunLog();
        }
    }
}
