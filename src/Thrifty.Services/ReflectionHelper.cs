using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Thrifty
{
    internal static class ReflectionHelper
    {
        public static IEnumerable<KeyValuePair<Type, T>> GetEffectiveClassAnnotations<T>(this Type type)
            where T : Attribute
        {
            T attribute = type.GetTypeInfo().GetCustomAttribute<T>();
            // if the class is directly annotated, it is considered the only annotation
            if (attribute  != null)
            {
                return new[] { new KeyValuePair<Type, T>(type, attribute) };
            }

            // otherwise find all annotations from all super classes and interfaces
            Dictionary<Type, T> builder = new Dictionary<Type, T>();
            AddEffectiveClassAnnotations<T>(type, builder);
            return builder;
        }

        private static void AddEffectiveClassAnnotations<T>(Type type, Dictionary<Type, T> builder)
            where T : Attribute
        {
            T attribute = type.GetTypeInfo().GetCustomAttribute<T>();
            if (attribute != null)
            {
                builder.Add(type, attribute);
                return;
            }
            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && !baseType.Equals(typeof(Object)))
            {
                AddEffectiveClassAnnotations<T>(baseType, builder);
            }
            var interfaces = type.GetInterfacesWithoutChain();
            foreach (var anInterface in interfaces)
            {
                AddEffectiveClassAnnotations<T>(anInterface, builder);
            }
        }

        private static Type[] GetInterfacesWithoutChain(this Type type)
        {
            var interfaces = type.GetTypeInfo().GetInterfaces().Except(new Type[] { type }).ToList();
            var expcet = interfaces.SelectMany(i => i.GetInterfaces().Except(new Type[] { i }));
            return interfaces.Except(expcet).ToArray();
        }

        public static IEnumerable<MethodInfo> FindAttributedMethods(this Type type, Type attribute)
        {
            List<MethodInfo> result = new List<MethodInfo>();

            // gather all publicly available methods
            // this returns everything, even if it's declared in a parent

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                // look for annotations recursively in super-classes or interfaces
                var managedMethod = FindAttributedMethods(
                        type,
                        attribute,
                        method.Name,
                        method.GetParameters().Select(p => p.ParameterType).ToArray());
                if (managedMethod != null)
                {
                    result.Add(managedMethod);
                }
            }

            return result;
        }

        public static MethodInfo FindAttributedMethods(this Type type, Type attribute, String methodName, params Type[] paramTypes)
        {
            try
            {
                var method = type.GetMethod(methodName, paramTypes);
                if (method != null && method.GetCustomAttribute(attribute) != null)
                {
                    return method;
                }
            }
            catch (ArgumentException) { }
            catch (AmbiguousMatchException) { }

            if (type.GetTypeInfo().BaseType != null)
            {
                var managedMethod = FindAttributedMethods(
                        type.GetTypeInfo().BaseType,
                        attribute,
                        methodName,
                        paramTypes);
                if (managedMethod != null)
                {
                    return managedMethod;
                }
            }

            foreach (var iface in type.GetTypeInfo().GetInterfaces())
            {
                var managedMethod = FindAttributedMethods(iface, attribute, methodName, paramTypes);
                if (managedMethod != null)
                {
                    return managedMethod;
                }
            }

            return null;
        }
    }
}
