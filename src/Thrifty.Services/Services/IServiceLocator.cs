using Thrifty.Nifty.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Thrifty.Services
{
    /// <summary>
    /// Define common behavior and help IoC containers define their factory.
    /// </summary>
    public interface IServiceLocator
    {
        /// <summary>
        /// Get the service with the type.
        /// </summary>
        /// <param name="context">rpc request context</param>
        /// <param name="serviceType">service type.</param>
        /// <returns></returns>
        object GetService(IRequestContext context, Type serviceType);
    }

    public class DelegateServiceLocator : IServiceLocator
    {
        private Func<IRequestContext, Type, Object> _factory;

        public DelegateServiceLocator(Func<IRequestContext, Type, Object> serviceFactory)
        {
            Guard.ArgumentNotNull(serviceFactory, nameof(serviceFactory));
            _factory = serviceFactory;
        }


        public object GetService(IRequestContext context, Type serviceType)
        {
            return _factory(context, serviceType);
        }
    }

    public class InstanceServiceLocator : IServiceLocator
    {
        private IEnumerable<Object> _instances;
        private Dictionary<Type, Object> _typeMapping = null;

        public InstanceServiceLocator(IEnumerable<Object> instances)
        {
            Guard.ArgumentNotNull(instances, nameof(instances));

            _typeMapping = new Dictionary<Type, object>();
            _instances = instances?.Where(i=>i != null)?.ToArray() ?? Enumerable.Empty<Object>();

            foreach (var i in _instances)
            {
                IEnumerable<Type> interfaces = i.GetType().GetTypeInfo().GetInterfaces()
                     .Where(face => face.GetTypeInfo().GetCustomAttribute<ThriftServiceAttribute>() != null);
                foreach (var type in interfaces)
                {
                    if (!_typeMapping.ContainsKey(type))
                    {
                        _typeMapping.Add(type, i);
                    }
                }
            }
        }

        public object GetService(IRequestContext context, Type serviceType)
        {
            Guard.ArgumentNotNull(serviceType, nameof(serviceType));

            if (_typeMapping.ContainsKey(serviceType))
            {
                return _typeMapping[serviceType];
            }
            throw new ThriftyServiceNotFoundException($"service '{serviceType.FullName}' can not be found on server.");
        }
    }
}
