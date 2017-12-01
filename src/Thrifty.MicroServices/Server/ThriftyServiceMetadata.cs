using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Thrifty.MicroServices.Commons;
using Newtonsoft.Json;
using Thrifty.Services.Metadata;

namespace Thrifty.MicroServices.Server
{
    public class ThriftyServiceMetadata
    {
        [JsonConstructor]
        public ThriftyServiceMetadata([JsonProperty("name")]string serviceName, [JsonProperty("version")]string version = "1.0.0")
        {
            Guard.ArgumentNotNullOrEmptyString(serviceName, nameof(serviceName));
            this.ServiceName = serviceName;
            this.Version = String.IsNullOrWhiteSpace(version) ? "1.0.0" : version;
        }

        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="serviceType">服务源类型</param>
        /// <param name="version">服务版本。</param>
        public ThriftyServiceMetadata(Type serviceType, string version = "1.0.0")
        {
            Guard.ArgumentNotNull(serviceType, nameof(serviceType));
            if (!serviceType.GetTypeInfo().IsInterface)
                throw new ThriftyException($"{serviceType.FullName} as a swifty service type must be a interface type.");

            ThriftServiceAttribute thriftServiceAttr = serviceType.GetTypeInfo().GetCustomAttribute<ThriftServiceAttribute>(false);
            if (thriftServiceAttr == null)
                throw new ThriftyException($"{serviceType.FullName} as a swifty service type must attrited by {nameof(ThriftServiceAttribute)}.");

            this.Version = String.IsNullOrWhiteSpace(version) ? "1.0.0" : version;
            this.ServiceName = ThriftServiceMetadata.ParseServiceName(serviceType);
        }

        /// <summary>
        /// 类型名
        /// </summary>
        [JsonProperty("name")]
        public string ServiceName { get; }

        /// <summary>
        ///     服务版本
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj == null || !obj.GetType().Equals(typeof(ThriftyServiceMetadata)))
            {
                return false;
            }

            ThriftyServiceMetadata that = (ThriftyServiceMetadata)obj;

            return String.Compare(this.ServiceName, that.ServiceName) == 0 && this.Version.Equals(that.Version);
        }

        public override int GetHashCode()
        {
            return ThriftyUtilities.Hash(this.ServiceName, this.Version);
        }

        public override string ToString()
        {
            return $"{this.ServiceName}:{this.Version}";
        }
    }
}