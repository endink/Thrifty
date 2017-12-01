using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ribbon.Test.Just4Fun
{
    public interface IContainer
    {
        T GetService<T>();
    }

    public interface ICalculator
    {
        int Add(int a, int b);
    }

    public static class Just4FunContext
    {
        private class DemoContainer : IContainer
        {
            private class Proxy<T> where T : IProcedure
            {
                private class RemoteProcedure : IRemoteProcedure
                {
                    public RemoteProcedure(string version, Type @interface, MethodInfo method)
                    {
                        Version = version;
                        Interface = @interface;
                        Method = method;
                    }
                    public string Version { get; }
                    public Type Interface { get; }
                    public MethodInfo Method { get; }
                }
                private class ProcedureParameter : IProcedureParameter
                {
                    public ProcedureParameter(string name, object value)
                    {
                        Name = name;
                        Value = value;
                    }
                    public string Name { get; }
                    public object Value { get; }
                }
                private class RealProxy : ICalculator
                {
                    private readonly string _version;
                    public RealProxy(string version)
                    {
                        _version = version;
                    }
                    private readonly IProcedureDiscovery _discovery = Container.GetService<IProcedureDiscovery>();
                    private readonly IServerSelector _selector = Container.GetService<IServerSelector>();
                    private readonly IRemoteProcedureCaller _caller = Container.GetService<IRemoteProcedureCaller>();
                    private readonly MethodInfo _addMethodInfo = typeof(ICalculator).GetMethod("Add");

                    public int Add(int a, int b)
                    {
                        var servers = _discovery.GetInstances<T>();//服务发现
                        var server = _selector.Select(servers);//负载均衡
                        var desc = new RemoteProcedure(_version, typeof(ICalculator), _addMethodInfo);//rpc描述
                        return (int)_caller.Call(server, desc, new IProcedureParameter[]
                           {
                            new ProcedureParameter(nameof(a), a),
                            new ProcedureParameter(nameof(b), b)
                           });//rpc调用
                    }
                }


                public T ToTarget()
                {
                    //build RealProxy 
                    return default(T);
                }
            }


            private static class DynamicProxy
            {
                public static T BuildProxy<T>(Type procedureType)
                {
                    dynamic obj = Activator.CreateInstance(typeof(Proxy<>).MakeGenericType(procedureType));
                    return obj.ToTarget();//动态代理
                }
            }


            public T GetService<T>()
            {
                if (typeof(T).GetInterfaces().Any(x => x == typeof(IProcedure)))
                    return DynamicProxy.BuildProxy<T>(typeof(T));
                return default(T);//DI
            }
        }
        public static readonly IContainer Container = new DemoContainer();

    }
}
