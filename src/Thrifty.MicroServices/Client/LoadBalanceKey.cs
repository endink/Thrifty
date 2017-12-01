using System;

namespace Thrifty.MicroServices.Client
{
    public class LoadBalanceKey : IEquatable<LoadBalanceKey>
    {
        public LoadBalanceKey(string version, string vipAddress)
        {
            Version = version;
            VipAddress = vipAddress;
        }

        public string Version { get; }

        public string VipAddress { get; }

        public bool Equals(LoadBalanceKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Version, other.Version) && string.Equals(VipAddress, other.VipAddress);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LoadBalanceKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Version != null ? Version.GetHashCode() : 0) * 397) ^ (VipAddress != null ? VipAddress.GetHashCode() : 0);
            }
        }
    }
}
