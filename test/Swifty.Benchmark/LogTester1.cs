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
    public class LogTester1 : LogTester
    {
        protected override int Port => 5555;


        [Benchmark(Description = "LogCase.GetMessages (Direct)")]
        public override void RunGetMessages()
        {
            base.RunGetMessages();
        }


        [Benchmark(Description = "LogCase.Log (Direct)")]
        public override void RunLog()
        {
            base.RunLog();
        }
    }
}
