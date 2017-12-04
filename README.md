# Thrifty Project

   
   
Thrifty.Net is a port of [Swift（from facebook）](https://github.com/facebook/swift) for .Net , an attribute-based library for creating Thrift serializable types and services.

you can write C# attributed object instead of IDL file and IDL generation cli.

### Thrifty = Thrift + Netty


[![Hex.pm](https://img.shields.io/hexpm/l/plug.svg)]()
[![nuget](https://img.shields.io/badge/nuget-coming%20soon-ff69b4.svg)]()

|       OS      | Testing |
|-------------|:----------:|
|**Linux**|[![test ok](https://img.shields.io/badge/eureka-testing%20pass-green.svg)]() [![test ok](https://img.shields.io/badge/end2end-testing%20pass-green.svg)]()|
|**Windows**  |[![test ok](https://img.shields.io/badge/eureka-testing%20pass-green.svg)]() [![test ok](https://img.shields.io/badge/end2end-testing%20pass-green.svg)]()|

## Thrifty Benchmark  

**end to end connection without connection pool**


```ini
BenchmarkDotNet=v0.10.8, OS=Windows 10 Redstone 1 (10.0.14393)
Processor=Intel Core i5-6300HQ CPU 2.30GHz (Skylake), ProcessorCount=4
Frequency=2250001 Hz, Resolution=444.4442 ns, Timer=TSC
dotnet cli version=1.0.4
  [Host]     : .NET Core 4.6.25211.01, 64bit RyuJIT [AttachedDebugger]
  DefaultJob : .NET Core 4.6.25211.01, 64bit RyuJIT
```

Method|Mean|Error|StdDev
------------------------------ |---------|----------|----------
'LogCase.Log (Direct)'|1.054 ms|0.0090 ms|0.0084 ms
'LogCase.GetMessages (Direct)'|1.047 ms|0.0109 ms|0.0091 ms

**locating services  use Eureka，without connection pool**

```ini
BenchmarkDotNet=v0.10.8, OS=Windows 10 Redstone 1 (10.0.14393)
Processor=Intel Core i5-6300HQ CPU 2.30GHz (Skylake), ProcessorCount=4
Frequency=2250001 Hz, Resolution=444.4442 ns, Timer=TSC
dotnet cli version=1.0.4
  [Host]     : .NET Core 4.6.25211.01, 64bit RyuJIT [AttachedDebugger]
  DefaultJob : .NET Core 4.6.25211.01, 64bit RyuJIT
```

 Method|Mean|Error|StdDev
------------------------------- |---------|----------|----------
'LogCase.Log (Eureka)'|1.104 ms|0.0210 ms|0.0207 ms
'LogCase.GetMessages (Eureka)'|1.108 ms|0.0182 ms|0.0161 ms

**locating services  use Eureka，with connection pool**
```ini
BenchmarkDotNet=v0.10.8, OS=Windows 10 Redstone 1 (10.0.14393)
Processor=Intel Core i5-6300HQ CPU 2.30GHz (Skylake), ProcessorCount=4
Frequency=2250001 Hz, Resolution=444.4442 ns, Timer=TSC
dotnet cli version=1.0.4
  [Host]     : .NET Core 4.6.25211.01, 64bit RyuJIT [AttachedDebugger]
  DefaultJob : .NET Core 4.6.25211.01, 64bit RyuJIT
```

Method|Mean|Error|StdDev|Median
 ------------------------------- |---------|---------|---------|---------
'LogCase.GetMessages (Eureka&Pool)' | 302.1 us | 6.023 us | 14.55 us | 298.5 us
'LogCase.Log (Eureka&Pool)' | 294.9 us | 6.789 us | 19.70 us | 289.2 us



# Serialization 

[Thrifty Codec](src/Thrifty.Services/Codecs) convert POCO to and from Thrift.
Thrifty support property、method、construction attributed. for example：

```csharp
    [ThriftStruct]
    public class LogEntry
    {

        [ThriftConstructor]
        public LogEntry([ThriftField(1)]String category, [ThriftField(2)]String message)
        {
            this.Category = category;
            this.Message = message;
        }

        [ThriftField(1)]
        public String Category { get; }

        [ThriftField(2)]
        public String Message { get; }
    }
```

# Service 

[Thrifty Service](src/Thrifty.Services/) attribute services to be exported with Thrift. For example:

```csharp
    [ThriftService("scribe")]
    public interface IScribe
    {
        [ThriftMethod("getMessages")]
        List<LogEntry> GetMessages();

        [ThriftMethod]
        ResultCode Log(List<LogEntry> messages);
    }

    public class Scribe : IScribe
    {
        public List<LogEntry> GetMessages()
        {
            return new List<LogEntry>
            {
                new LogEntry { Category = "c1", Message = Guid.NewGuid().ToString() },
                new LogEntry { Category = "c2", Message = Guid.NewGuid().ToString() },
                new LogEntry { Category = "c3", Message = Guid.NewGuid().ToString() }
            };
        }

        public ResultCode Log(List<LogEntry> messages)
        {
            return ResultCode.TRY_LATER;
        }
    }
```


# Start Server

```csharp
 var factory = new LoggerFactory();
            factory.AddConsole(LogLevel.Debug);
            var serverConfig = new ThriftyServerOptions
            {
                QueueTimeout = TimeSpan.FromMinutes(1),
                TaskExpirationTimeout = TimeSpan.FromMinutes(1),
                ConnectionLimit = 10000
            };


            var bootStrap = new ThriftyBootstrap(new object[] { new Scribe() },
                serverConfig, new InstanceDescription("Sample", "EurekaInstance1", "127.0.0.1"), factory);

            bootStrap
                .SslConfig(new SslConfig
                {
                    CertFile = "server.pfx",
                    CertPassword = "abc@123",
                    CertFileProvider = new EmbeddedFileProvider(typeof(Program).GetTypeInfo().Assembly)
                })
               .AddService(typeof(IScribe), version: "1.0.0")
               //true to register into eureka , disable eureka , set to false
               .EurekaConfig(true, 
                             new EurekaClientConfig { EurekaServerServiceUrls = "http://192.168.0.10:8761/eureka" })
               // bind any
               .Bind(IPAddress.Any.ToString(), 3366)
               .StartAsync();
```

# Use Client

```csharp
var factory = new LoggerFactory();
using (var client = new ThriftyClient(new ThriftyClientOptions
{
    LoggerFactory = factory,
    ConnectionPoolEnabled = true, // default is true
    EurekaEnabled = true, //default is true
    Eureka = new ThriftyClientEurekaConfig { EurekaServerServiceUrls = "http://192.168.0.10:8761/eureka" } //optional
}))
{
    /** *************if without eureka:*****************
     * 
        var service = client.Create<Thrifty.IScribe>("127.0.0.1:3366",
        new ClientSslConfig
        {
            CertFile = "ca.crt",
            FileProvider = new EmbeddedFileProvider(typeof(ClientProgram).GetTypeInfo().Assembly)
        });
     */
    var service = client.Create<Thrifty.IScribe>("1.0.0", "EurekaInstance1",
        new ClientSslConfig
        {
            CertFile = "ca.crt",
            FileProvider = new EmbeddedFileProvider(typeof(ClientProgram).GetTypeInfo().Assembly)
        });
    var logs = service.GetMessages();
    ...
}
```



# Documents

document is [here](https://github.com/endink/Thrifty/wiki) (Only Chinese documents are available now).

