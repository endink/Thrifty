using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Thrifty
{
    public class TaskCompletionSourceEx
    {
        private static readonly ConcurrentDictionary<Type, Methods> TaskResultMethods = new ConcurrentDictionary<Type, Methods>();

        private Object _source = null;
        private Methods _methods = null;

        public TaskCompletionSourceEx(Type resultType)
        {
            Guard.ArgumentNotNull(resultType, nameof(resultType));
            var sourceType = typeof(TaskCompletionSource<>).MakeGenericType(resultType);
            _source = Activator.CreateInstance(sourceType);

            _methods = TaskResultMethods.GetOrAdd(sourceType, t =>
            {
                Methods methods = new Methods();

                var soureParam = Expression.Parameter(t);
                var result = Expression.Parameter(resultType);

                var method1 = Expression.Call(soureParam,
                            t.GetTypeInfo().GetMethod(nameof(TaskCompletionSource<Object>.TrySetResult)),
                            result);

                var exception = Expression.Parameter(typeof(Exception));
                var method2 = Expression.Call(soureParam,
                            t.GetTypeInfo().GetMethod(nameof(TaskCompletionSource<Object>.TrySetException), new Type[] { typeof(Exception) }),
                            exception);

                var propertyCall = Expression.MakeMemberAccess(soureParam, t.GetProperty(nameof(TaskCompletionSource<Object>.Task)));

                methods.TrySetResultMethod = Expression.Lambda(method1, soureParam, result).Compile();
                methods.TrySetExceptionMethod = Expression.Lambda(method2, soureParam, exception).Compile();
                methods.TaskProperty = Expression.Lambda(propertyCall, soureParam).Compile();

                return methods;
            });
        }

        public bool TrySetException(Exception exception)
        {
            return (bool)_methods.TrySetExceptionMethod.DynamicInvoke(_source, exception);
        }

        public bool TrySetResult(Object result)
        {
            return (bool)_methods.TrySetResultMethod.DynamicInvoke(_source, result);
        }

        public Task Task
        {
            get { return (Task)_methods.TaskProperty.DynamicInvoke(_source); }
        }

        private class Methods
        {
            public Delegate TrySetExceptionMethod { get; set; }

            public Delegate TrySetResultMethod { get; set; }

            public Delegate TaskProperty { get; set; }
        }
    }
}
