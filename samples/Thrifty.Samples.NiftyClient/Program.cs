using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Thrifty.MicroServices.Client;
using Thrifty.Nifty.Client;
using Thrifty.Samples.Common;
using Thrifty.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Thrifty.Samples
{

    public class ClientProgram
    {
        public static void Main(string[] args)
        {
            var factory = new LoggerFactory();
            using (var client = new ThriftyClient(new ThriftyClientOptions
            {
                LoggerFactory = factory,
                ConnectionPoolEnabled = false
            }))
            {
                var service = client.Create<IService>("127.0.0.1:9999", new ClientSslConfig
                {
                    CertFile = "ca.crt",
                    FileProvider = new EmbeddedFileProvider(typeof(ClientProgram).GetTypeInfo().Assembly)
                });
                for (var i = 0; i < 1024 * 1024; i++)
                {
                    var x = i;
                    Console.WriteLine($" {x} Invoke");
                    TestAll(service);
                }
            }
            Console.WriteLine("Finish");
            Console.ReadKey();
        }

        //private static void Run(object state)
        //{
        //    var client = (ThriftyClient)state;
        //    var service = client.Create<IService>("0.0.1", "TestApp");
        //    for (var i = 0; i < 1024 * 1024; i++)
        //    {
        //        var x = i;
        //        Console.WriteLine($" {x} Invoke");
        //        TestAll(service);
        //    }
        //}

        private static void TestAll(IService service)
        {
            var beginTime = DateTime.Now;
            var entities = service.AllEntities();
            if ((entities?.Length ?? 0) == 0) throw new Exception("AllEntities");
            if (!entities[0].Guid.HasValue) throw new Exception("AllEntities,guid is null");
            if (entities[0].GuidArray.Length != 2) throw new Exception("AllEntities,guidArray length is erro");
            var x = service.ChangeDay(entities[1], DayOfWeek.Wednesday);
            if (x.Day != DayOfWeek.Wednesday) throw new Exception("ChangeDay");
            x = service.ChangeBoolValue(entities[1], false);
            if (x.BoolValue) throw new Exception("ChangeBoolValue");
            x = service.ChangeByteNumber(entities[1], 123);
            if (x.ByteNumber != 123) throw new Exception("ChangeByteNumber");
            x = service.ChangeShortNumber(entities[1], 1234);
            if (x.ShortNumber != 1234) throw new Exception("ChangeShortNumber");
            x = service.ChangeIntNumber(entities[1], 12345);
            if (x.IntNumber != 12345) throw new Exception("ChangeIntNumber");
            x = service.ChangeLongNumber(entities[1], 123456);
            if (x.LongNumber != 123456) throw new Exception("ChangeLongNumber");
            x = service.ChangeDoubleNumber(entities[1], 123456.7);
            if (x.DoubleNumber != 123456.7) throw new Exception("ChangeDoubleNumber");
            x = service.ChangeStringValue(entities[1], "abcdefg");
            if (x.StringValue != "abcdefg") throw new Exception("ChangeDoubleNumber");
            var date = new DateTime(1988, 11, 12, 8, 10, 1).ToUniversalTime();
            x = service.ChangeDateTime(entities[1], date);
            if (x.Now != date) throw new Exception("ChangeDateTime");

            var count = service.GetCount(entities);
            if (count != entities.Length) throw new Exception("GetCount");
            count = service.GetMatchedDay(DayOfWeek.Monday, entities);
            if (count != entities.Count(m => m.Day == DayOfWeek.Monday)) throw new Exception("GetMatchedDay");
            count = service.GetMatchedBoolValue(true, entities);
            if (count != entities.Count(m => m.BoolValue)) throw new Exception("GetMatchedBoolValue");
            count = service.GetMatchedByteNumber(123, entities);
            if (count != entities.Count(m => m.ByteNumber == 123)) throw new Exception("GetMatchedByteNumber");
            count = service.GetMatchedShortNumber(1234, entities);
            if (count != entities.Count(m => m.ShortNumber == 1234)) throw new Exception("GetMatchedShortNumber");
            count = service.GetMatchedIntNumber(12345, entities);
            if (count != entities.Count(m => m.IntNumber == 12345)) throw new Exception("GetMatchedIntNumber");
            count = service.GetMatchedLongNumber(123456, entities);
            if (count != entities.Count(m => m.LongNumber == 123456)) throw new Exception("GetMatchedLongNumber");
            count = service.GetMatchedDoubleNumber(123456.7, entities);
            if (count != entities.Count(m => m.DoubleNumber == 123456.7)) throw new Exception("GetMatchedDoubleNumber");
            count = service.GetMatchedStringValue("abc", entities);
            if (count != entities.Count(m => m.StringValue == "abc")) throw new Exception("GetMatchedStringValue");


            service.SerializeMatches(DayOfWeek.Monday, entities);
            service.Serialize(entities);
            service.SerializeMatchesBoolValue(true, entities);
            service.SerializeMatchesByteNumber(123, entities);
            service.SerializeMatchesShortNumber(1234, entities);
            service.SerializeMatchesIntNumber(12345, entities);
            service.SerializeMatchesLongNumber(123456, entities);
            service.SerializeMatchesDoubleNumber(123456.7, entities);
            service.SerializeMatchesStringValue("abc", entities);

            var value = service.ThrowExceptionWithReturnValue(false);
            if (value != 123456) throw new Exception("ThrowExceptionWithReturnValue");
            service.ThrowException();
            try
            {
                service.ThrowExceptionWithReturnValue(true);
                throw new Exception("Exception,ThrowExceptionWithReturnValue");
            }
            catch (Exception e)
            {
                if (e.Message == "Exception,ThrowExceptionWithReturnValue")
                    throw new Exception("ThrowExceptionWithReturnValue");
            }


            var endTime = DateTime.Now;

            Console.WriteLine($"!=!=!=!=!=Thread Id:{Thread.CurrentThread.ManagedThreadId} execute all methods,spend {(endTime - beginTime).TotalMilliseconds},avg time:{(endTime - beginTime).TotalMilliseconds / 31}");
        }
    }
}

