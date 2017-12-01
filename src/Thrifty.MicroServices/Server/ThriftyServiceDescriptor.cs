using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrifty.MicroServices.Server
{
    public class ThriftyServiceDescriptor : IEquatable<ThriftyServiceDescriptor>
    {
        public ThriftyServiceDescriptor(Type serviceType, string version = "1.0.0")
        {
            Guard.ArgumentNotNull(serviceType, nameof(serviceType));
            this.ServiceType = serviceType;
            this.Metadata = new ThriftyServiceMetadata(serviceType, version);
        }

        public Type ServiceType { get; }

        public ThriftyServiceMetadata Metadata { get; }

        public bool Equals(ThriftyServiceDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(ServiceType, other.ServiceType) && Equals(Metadata, other.Metadata);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ThriftyServiceDescriptor)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ServiceType != null ? ServiceType.GetHashCode() : 0) * 397) ^ (Metadata != null ? Metadata.GetHashCode() : 0);
            }
        }
    }
}
