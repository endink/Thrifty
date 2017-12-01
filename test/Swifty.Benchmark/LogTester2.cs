using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;
using Microsoft.Extensions.Logging;
using Thrifty.MicroServices.Client;
using Thrifty.MicroServices.Commons;
using Thrifty.MicroServices.Server;
using Thrifty.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.Benchmark
{
    public class LogTester2 : LogTester
    {
        protected override int Port => 6666;

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
