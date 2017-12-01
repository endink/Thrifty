# Thrifty Project

   
   
Thrifty.Net is a port of [Swift（from facebook）](https://github.com/facebook/swift) for .Net , an attribute-based library for creating Thrift serializable types and services.

you can write C# attributed object instead of IDL file and IDL generation cli.

### Thrifty = Thrift + Netty

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
    public class InMemoryScribe
    {
        private readonly List<LogEntry> messages = new List<LogEntry>();

        public List<LogEntry> GetMessages()
        {
            return messages;
        }

        [ThriftMethod("Log")]
        public ResultCode Log(List<LogEntry> messages)
        {
            this.messages.AddRange(messages);
            return ResultCode.OK;
        }
    }
```


# Documents

document is [here](https://github.com/endink/Thrifty/wiki) (Only Chinese documents are available now).

