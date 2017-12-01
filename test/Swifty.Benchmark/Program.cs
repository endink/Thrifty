using BenchmarkDotNet.Running;
using System;
using System.Reflection;
using System.Text;

namespace Thrifty.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            ConsoleKeyInfo info;
            Console.WriteLine("按任意键开始测试， 退出按 Esc");
            while ((info = Console.ReadKey()) != null)
            {
                switch (info)
                {
                    case ConsoleKeyInfo n when n.Key == ConsoleKey.Escape:
                        break;
                    default:
                        BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
                        break;
                }
                Console.WriteLine("按任意键开始测试， 退出按 Esc");
            }
        }

        
    }
}