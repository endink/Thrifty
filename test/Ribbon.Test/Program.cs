#define _USE_PYTHON_SERVER
using System;
using System.Diagnostics;
using System.Linq; 
using Thrifty.MicroServices.Ribbon;


namespace Ribbon.Test
{
    public class Program
    {
        public static void Main(string[] args)
        { 
            const int serverCount = 40;
            var servers = (from port in Enumerable.Range(50000, serverCount)
                           select new Server("localhost", port)).ToList();
            const int count = 100 * 100;
            const int expect = count / serverCount;
#if !USE_PYTHON_SERVER
            var httpServers = from server in servers select new HttpServer(server);
#endif

            try
            {
#if !USE_PYTHON_SERVER
                foreach (var httpServer in httpServers)
                {
                    httpServer.Start();
                }
#endif
                var items = from type in typeof(Program).Assembly.GetTypes()
                            where type.GetInterface("ITest") != null && !type.IsAbstract
                            select Activator.CreateInstance(type) as ITest;
                foreach (var item in items)
                {
                    var watch = Stopwatch.StartNew();
                    var result = item.Run(count, servers);
                    Render(item.Name, result, watch.ElapsedMilliseconds, expect);
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
            finally
            {
#if !USE_PYTHON_SERVER
                foreach (var httpServer in httpServers)
                {
                    httpServer.Dispose();
                }
#endif
            }
        }


        private static void ColorWrite(object value, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(value);
        }
        private static void ColorWriteKeyValue(string key, object value)
        {
            ColorWrite(key, ConsoleColor.Green);
            ColorWrite(" : ", ConsoleColor.White);
            ColorWrite(value, ConsoleColor.Gray);
        }
        private static void Render(string name, ServerHit[] hits, long elapsedMilliseconds, int expect)
        {
            var length = "ElapsedMilliseconds".Length;
            Console.WriteLine();
            ColorWriteKeyValue("ElapsedMilliseconds", $"{elapsedMilliseconds}ms");
            Console.WriteLine();
            ColorWriteKeyValue("Rule".PadLeft(length), name);
            Console.WriteLine();
            ColorWriteKeyValue("Min".PadLeft(length), hits.Min(h => h.HitCount));
            Console.WriteLine();
            ColorWriteKeyValue("Max".PadLeft(length), hits.Max(h => h.HitCount));
            Console.WriteLine();
            ColorWriteKeyValue("Avg".PadLeft(length), $"{hits.Average(h => h.HitCount)}/{expect}");

            int total = hits.Sum(h => h.HitCount);
            foreach (var hit in hits.OrderBy(x => x.Server.Port))
            {
                Console.WriteLine();
                double rate = ((double)hit.HitCount / total);

                ColorWriteKeyValue(hit.Server.ToString().PadLeft(length), $"{hit.HitCount}    ({rate.ToString("P2")})");
            }
        }
    }
}
