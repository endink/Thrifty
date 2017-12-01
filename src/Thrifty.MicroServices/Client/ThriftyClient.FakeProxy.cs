using Thrifty.Nifty.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Thrifty.MicroServices.Client
{
    partial class ThriftyClient
    {
        /// <summary>
        /// 伪代理
        /// 在动态程序集Thrifty.MicroServices.DynamicAssembly中动态创建接口的代理
        /// 接口中不能包含事件和属性
        /// </summary> 
        private static class FakeProxy<T> where T : class
        {
            private static readonly Func<ThriftyClient, ClientSslConfig, Ribbon.Server, string, string, T> Creator;
            private static TM[] Find<TM>(Type interfaceType, Func<Type, TM[]> finder)
            {
                var list = new List<TM>(finder(interfaceType));
                var items = from item in interfaceType.GetInterfaces() where item != typeof(object) select item;
                foreach (var item in items) list.AddRange(Find(item, finder));
                return list.ToArray();
            }
            private static MethodInfo[] FindInterfaceMethods(Type interfaceType) => Find(interfaceType, t =>
            {
                var events = (from @event in t.GetEvents()
                              select new[] { @event.GetRemoveMethod(), @event.GetAddMethod() })
                    .Aggregate(new List<MethodInfo>(), (x, p) =>
                    {
                        x.AddRange(p);
                        return x;
                    }).ToList();
                const BindingFlags attrs = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
                return (from method in t.GetMethods(attrs)
                        where !events.Contains(method)
                        select method).ToArray();
            });
            private static void Ldarg(ILGenerator il, int index)
            {
                switch (index)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        il.Emit(index < 255 ? OpCodes.Ldarg_S : OpCodes.Ldarg, index + 1);
                        break;
                }
            }
            private static void Ldc_I4(ILGenerator il, int index)
            {
                if (index < 0) il.Emit(OpCodes.Ldc_I4_M1);
                else
                    switch (index)
                    {
                        case 0:
                            il.Emit(OpCodes.Ldc_I4_0);
                            break;
                        case 1:
                            il.Emit(OpCodes.Ldc_I4_1);
                            break;
                        case 2:
                            il.Emit(OpCodes.Ldc_I4_2);
                            break;
                        case 3:
                            il.Emit(OpCodes.Ldc_I4_3);
                            break;
                        case 4:
                            il.Emit(OpCodes.Ldc_I4_4);
                            break;
                        case 6:
                            il.Emit(OpCodes.Ldc_I4_6);
                            break;
                        case 7:
                            il.Emit(OpCodes.Ldc_I4_7);
                            break;
                        case 8:
                            il.Emit(OpCodes.Ldc_I4_8);
                            break;
                        default:
                            il.Emit(OpCodes.Ldc_I4_S, index);
                            break;
                    }
            }
            private static void CreateMethod(TypeBuilder typeBuilder, MethodInfo methodInfo, FieldBuilder field, FieldBuilder ssl, FieldBuilder caller, FieldBuilder server, FieldBuilder version, FieldBuilder vipAddress)
            {
                var parameters = methodInfo.GetParameters();
                var length = parameters?.Length ?? 0;
                var parameterTypes = length == 0 ? Type.EmptyTypes : parameters.Select(p => p.ParameterType).ToArray();
                var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final,
                    methodInfo.ReturnType, parameterTypes);
                var items = methodInfo.CustomAttributes;
                foreach (var item in items) methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(item.Constructor, item.ConstructorArguments.Select(x => x.Value).ToArray()));
                for (var i = 0; i < length && length > 0; i++)
                {
                    var parameter = parameters[i];
                    var parameterBuilder = methodBuilder.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
                    if (parameter.HasDefaultValue) parameterBuilder.SetConstant(parameter.DefaultValue);
                }
                var il = methodBuilder.GetILGenerator();
                il.DeclareLocal(typeof(object[]));//object[] array;
                if (length > 0)
                {
                    Ldc_I4(il, length);//引入数组长度
                    il.Emit(OpCodes.Newarr, typeof(object));//new object[2];
                    il.Emit(OpCodes.Stloc_0);//array=new object[2]; 
                    for (var i = 0; i < length; i++)
                    {
                        il.Emit(OpCodes.Ldloc_0);//array
                        Ldc_I4(il, i);//array[i]
                        Ldarg(il, i);//某个位置的参数
                        var parameterType = parameters[i].ParameterType;
                        if (parameterType.GetTypeInfo().IsValueType) il.Emit(OpCodes.Box, parameterType);
                        il.Emit(OpCodes.Stelem_Ref);//array[i]=某个位置的参数
                    }
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stloc_0);
                }
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, caller);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ldtoken, methodInfo);
                il.Emit(OpCodes.Call, GetMethodFromHandle);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, ssl);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, server);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, version);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, vipAddress);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Callvirt, CallMethodInfo);
                var returnType = methodInfo.ReturnType;
                if (returnType == typeof(void))
                    il.Emit(OpCodes.Pop);
                else if (returnType.GetTypeInfo().IsValueType) il.Emit(OpCodes.Unbox_Any, returnType);
                else il.Emit(OpCodes.Castclass, returnType);
                il.Emit(OpCodes.Ret);
            }

            static FakeProxy()
            {
                var interfaceType = typeof(T);
                var name = $"{interfaceType.FullName}<Proxy>";
                var type = BuildType(name, interfaceType);
                var p1 = Expression.Parameter(typeof(ThriftyClient));
                var p2 = Expression.Parameter(typeof(ClientSslConfig));
                var p3 = Expression.Parameter(typeof(Ribbon.Server));
                var p4 = Expression.Parameter(typeof(string));
                var p5 = Expression.Parameter(typeof(string));
                var p6 = Expression.Parameter(typeof(Func<ThriftyClient, MethodInfo, ClientSslConfig, Ribbon.Server, string, string, object[], object>));
                var ctor = type.GetConstructor(new[] { typeof(ThriftyClient), typeof(ClientSslConfig),typeof(Ribbon.Server), typeof(string), typeof(string),
                    typeof(Func<ThriftyClient, MethodInfo, ClientSslConfig, Ribbon.Server, string, string, object[], object>) });
                if (ctor == null) throw new ThriftyException("An unknown error occurred!");

                var newObject = Expression.New(ctor, p1, p2, p3, p4, p5, p6);

                var func = Expression.Lambda<Func<ThriftyClient, ClientSslConfig, Ribbon.Server, string, string,
                    Func<ThriftyClient, MethodInfo, ClientSslConfig, Ribbon.Server, string, string, object[], object>, T>>
                    (newObject, p1, p2, p3, p4, p5, p6).Compile();

                Creator = (sc, ssl, server, verison, vip) => func(sc, ssl, server, verison, vip, CrossCaller);
            }

            private static object CrossCaller(ThriftyClient client, MethodBase info, ClientSslConfig sslConfig,
                Ribbon.Server server, string version, string vipAddress, object[] args) =>
                client.Call(info as MethodInfo, sslConfig, server, version, vipAddress, args);

            private static Type BuildType(string name, Type interfaceType)
            {
                var typeBuilder = ModuleBuilder.DefineType(name,
                    TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public, typeof(object),
                    new[] { interfaceType });
                var field = typeBuilder.DefineField("_client", typeof(ThriftyClient), FieldAttributes.Private | FieldAttributes.InitOnly);
                var ssl = typeBuilder.DefineField("_ssl", typeof(ClientSslConfig), FieldAttributes.Private | FieldAttributes.InitOnly);
                var caller = typeBuilder.DefineField("_caller", typeof(Func<ThriftyClient, MethodInfo, ClientSslConfig, Ribbon.Server, string, string, object[], object[], object>), FieldAttributes.Private | FieldAttributes.InitOnly);
                var server = typeBuilder.DefineField("_server", typeof(Ribbon.Server), FieldAttributes.Private | FieldAttributes.InitOnly);
                var version = typeBuilder.DefineField("_version", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);
                var vipAddress = typeBuilder.DefineField("_vipAddress", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);

                field.SetCustomAttribute(new CustomAttributeBuilder(DebuggerBrowsableConstructor, new object[] { DebuggerBrowsableState.Never }));
                ssl.SetCustomAttribute(new CustomAttributeBuilder(DebuggerBrowsableConstructor, new object[] { DebuggerBrowsableState.Never }));
                caller.SetCustomAttribute(new CustomAttributeBuilder(DebuggerBrowsableConstructor, new object[] { DebuggerBrowsableState.Never }));
                server.SetCustomAttribute(new CustomAttributeBuilder(DebuggerBrowsableConstructor, new object[] { DebuggerBrowsableState.Never }));
                version.SetCustomAttribute(new CustomAttributeBuilder(DebuggerBrowsableConstructor, new object[] { DebuggerBrowsableState.Never }));
                vipAddress.SetCustomAttribute(new CustomAttributeBuilder(DebuggerBrowsableConstructor, new object[] { DebuggerBrowsableState.Never }));

                var items = interfaceType.GetTypeInfo().CustomAttributes;
                foreach (var item in items) typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(item.Constructor, item.ConstructorArguments.Select(x => x.Value).ToArray()));
                var methods = FindInterfaceMethods(interfaceType);
                foreach (var method in methods) CreateMethod(typeBuilder, method, field, ssl, caller, server, version, vipAddress);
                var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                    new[] { typeof(ThriftyClient), typeof(ClientSslConfig), typeof(Ribbon.Server), typeof(string), typeof(string),
                        typeof(Func<ThriftyClient, MethodInfo, ClientSslConfig, Ribbon.Server, string, string, object[], object>) });
                var il = constructor.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stfld, ssl);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Stfld, server);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_S, 4);
                il.Emit(OpCodes.Stfld, version);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_S, 5);
                il.Emit(OpCodes.Stfld, vipAddress);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_S, 6);
                il.Emit(OpCodes.Stfld, caller);
                il.Emit(OpCodes.Ret);
                var type = typeBuilder.CreateTypeInfo().AsType();
                return type;
            }

            public static T Create(ThriftyClient client, Ribbon.Server server, string version, string vipAddress, ClientSslConfig sslConfig) => Creator(client, sslConfig, server, version, vipAddress);
        }
    }
}
