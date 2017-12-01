using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Thrifty.Nifty.Client;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Thrifty.Services.Services.DynamicProxy;

namespace Thrifty.Services
{
    public interface IFake
    {
        object Invoke(MethodInfo method, object[] args);
    }

    internal class DynamicProxyClientFactory : IThriftClientFactory
    {
        private static class Creator
        {
            private static readonly ModuleBuilder ModuleBuilder;
            private static readonly MethodInfo Invoke = typeof(IFake).GetMethod(nameof(IFake.Invoke));
            private static readonly MethodInfo CallMethodInfo = typeof(Func<IFake, MethodBase, object[], object>).GetMethod(nameof(IFake.Invoke));
            private static readonly MethodInfo GetMethodFromHandle = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new[] { typeof(RuntimeMethodHandle) });
            private static readonly MethodInfo[] ObjectDefaultMethodInfos =
                {
                    typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object) }),
                    typeof(object).GetMethod(nameof(object.GetHashCode), Type.EmptyTypes),
                    typeof(object).GetMethod(nameof(object.ToString), Type.EmptyTypes)
                };

            private static readonly ConstructorInfo DebuggerBrowsableConstructor = typeof(DebuggerBrowsableAttribute).GetConstructor(new[] { typeof(DebuggerBrowsableState) });


            private class ProxyBuilder
            {
                private readonly Lazy<Func<IFake, object>> _creator;
                private readonly Type _clientType;
                private static TM[] Find<TM>(Type interfaceType, Func<Type, TM[]> finder)
                {
                    var list = new List<TM>(finder(interfaceType));
                    var items = from item in interfaceType.GetInterfaces() select item;
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
                        }).ToArray();

                    const BindingFlags attrs = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;

                    return (from method in t.GetMethods(attrs)
                            where !events.Contains(method)
                            select method).ToArray();

                }).Distinct().ToArray();

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
                private static void CreateMethod(TypeBuilder typeBuilder, MethodInfo methodInfo, FieldBuilder field)
                {
                    var parameters = methodInfo.GetParameters();
                    var length = parameters?.Length ?? 0;
                    var parameterTypes = length == 0 ? Type.EmptyTypes : parameters.Select(p => p.ParameterType).ToArray();
                    var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final,
                        methodInfo.ReturnType, parameterTypes);
                    var items = methodInfo.CustomAttributes;

                    foreach (var item in items)
                    {
                        var args = item.ConstructorArguments.Select(x => x.Value).ToArray();
                        methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(item.Constructor, args));
                    }

                    for (var i = 0; i < length && length > 0; i++)
                    {
                        var parameter = parameters[i];
                        var parameterBuilder = methodBuilder.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
                        if (parameter.HasDefaultValue) parameterBuilder.SetConstant(parameter.DefaultValue);
                    }

                    var il = methodBuilder.GetILGenerator();
                    il.DeclareLocal(typeof(object[]));
                    if (length > 0)
                    {
                        Ldc_I4(il, length);
                        il.Emit(OpCodes.Newarr, typeof(object));
                        il.Emit(OpCodes.Stloc_0);
                        for (var i = 0; i < length; i++)
                        {
                            il.Emit(OpCodes.Ldloc_0);
                            Ldc_I4(il, i);
                            Ldarg(il, i);
                            var parameterType = parameters[i].ParameterType;
                            if (parameterType.GetTypeInfo().IsValueType) il.Emit(OpCodes.Box, parameterType);
                            il.Emit(OpCodes.Stelem_Ref);
                        }
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Stloc_0);
                    }
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);
                    il.Emit(OpCodes.Ldtoken, methodInfo);
                    il.Emit(OpCodes.Call, GetMethodFromHandle);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Callvirt, Invoke);
                    var returnType = methodInfo.ReturnType;

                    if (returnType == typeof(void))
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    else if (returnType.GetTypeInfo().IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, returnType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, returnType);
                    }
                    il.Emit(OpCodes.Ret);
                }
                private static void CreateObjectMethod(TypeBuilder typeBuilder, MethodInfo methodInfo, FieldBuilder field)
                {
                    var parameters = methodInfo.GetParameters();
                    var length = parameters?.Length ?? 0;
                    var parameterTypes = length == 0 ? Type.EmptyTypes : parameters.Select(p => p.ParameterType).ToArray();
                    const MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
                    var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, attrs, methodInfo.ReturnType, parameterTypes);
                    for (var i = 0; i < length && length > 0; i++)
                    {
                        var parameter = parameters[i];
                        methodBuilder.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
                    }
                    var il = methodBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);

                    for (var i = 0; i < length && length > 0; i++)
                    {
                        Ldarg(il, i);
                    }

                    il.Emit(OpCodes.Callvirt, methodInfo);
                    il.Emit(OpCodes.Ret);
                }

                public ProxyBuilder(Type clientType)
                {
                    _clientType = clientType;
                    _creator = new Lazy<Func<IFake, object>>(BuildCreator, true);
                }

                private static MethodInfo[] PrepareMethods(Type clientType, bool isDisposable)
                {
                    if (isDisposable)
                    {
                        return FindInterfaceMethods(clientType)
                            .Concat(FindInterfaceMethods(typeof(INiftyClientChannelAware)))
                            .OrderBy(x => x.Name).ToArray();
                    }
                    else
                    {
                        return FindInterfaceMethods(clientType)
                             .Concat(FindInterfaceMethods(typeof(INiftyClientChannelAware)))
                             .Concat(FindInterfaceMethods(typeof(IDisposable)))
                             .OrderBy(x => x.Name).ToArray();
                    }
                }
                private Func<IFake, object> BuildCreator()
                {
                    var clientType = _clientType;
                    var name = $"{clientType.FullName}<Proxy>";

                    var disposable = typeof(IDisposable).GetTypeInfo().IsAssignableFrom(clientType);
                    //find interfaces

                    var interfaces = disposable
                        ? new[] { clientType, typeof(INiftyClientChannelAware) }
                        : new[] { clientType, typeof(INiftyClientChannelAware), typeof(IDisposable) };

                    //default type
                    var typeBuilder = ModuleBuilder.DefineType(name, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public, typeof(object), interfaces);
                    var items = clientType.GetTypeInfo().CustomAttributes;
                    foreach (var item in items)
                    {
                        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(item.Constructor, item.ConstructorArguments.Select(x => x.Value).ToArray()));
                    }
                    //define fake memeber field
                    var field = typeBuilder.DefineField("_fake", typeof(IFake), FieldAttributes.Private | FieldAttributes.InitOnly);
                    field.SetCustomAttribute(new CustomAttributeBuilder(DebuggerBrowsableConstructor, new object[] { DebuggerBrowsableState.Never }));

                    //declare constructor
                    /* 
                    ClassA
                    {
                        private readonly IFake _fake;
                        ClassA(IFake f1)
                        {
                            _fake = f1;
                        }
                    }
                    */
                    var constructor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                        new[] { typeof(IFake) });

                    var il = constructor.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Stfld, field);
                    il.Emit(OpCodes.Ret);


                    //declare methods
                    var methods = PrepareMethods(clientType, disposable);

                    foreach (var item in ObjectDefaultMethodInfos)
                    {
                        CreateObjectMethod(typeBuilder, item, field);
                    }
                    foreach (var method in methods)
                    {
                        CreateMethod(typeBuilder, method, field);
                    }

                    var type = typeBuilder.CreateTypeInfo().AsType();
                    var p1 = Expression.Parameter(typeof(IFake));
                    var ctor = type.GetConstructor(new[] { typeof(IFake) });
                    if (ctor == null)
                    {
                        throw new ThriftyException("An unknown error occurred!");
                    }
                    return Expression.Lambda<Func<IFake, object>>(Expression.New(ctor, p1), p1).Compile();
                }
                public object Build(IFake fake)
                {
                    var weak = new WeakReference(_creator.Value(fake), false);
                    return weak.Target;
                }
            }
            static Creator()
            {
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Thrifty.Services.DynamicAssembly"),
                    AssemblyBuilderAccess.Run);
                ModuleBuilder = assemblyBuilder.DefineDynamicModule("main");
            }
            private static readonly Dictionary<Type, ProxyBuilder> Dictionary = new Dictionary<Type, ProxyBuilder>();
            private static readonly ConcurrentDictionary<Type, object> Syncs = new ConcurrentDictionary<Type, object>();
            public static object Create(Type clientType, IFake fake)
            {
                if (!Dictionary.TryGetValue(clientType, out ProxyBuilder builder))
                {
                    lock (Syncs.GetOrAdd(clientType, k => new object()))
                    {
                        if (!Dictionary.TryGetValue(clientType, out builder))
                        {
                            builder = new ProxyBuilder(clientType);
                            Dictionary.Add(clientType, builder);
                        }
                    }
                }
                return builder.Build(fake);
            }
        }



        public object CreateClient(INiftyClientChannel channel, Type clientType, ThriftClientMetadata clientMetadata, IEnumerable<ThriftClientEventHandler> clientHandlers, string clientDescription)
        {
            var fake = new Fake(clientDescription, clientMetadata, channel,
                ImmutableList.CreateRange(clientHandlers).ToImmutableList());
            var instance = Creator.Create(clientType, fake);
            return instance;
        }
    }

}
