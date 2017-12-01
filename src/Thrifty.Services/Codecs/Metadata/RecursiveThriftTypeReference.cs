using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public class RecursiveThriftTypeReference : IThriftTypeReference
    {
        private ThriftCatalog catalog;

        public RecursiveThriftTypeReference(ThriftCatalog catalog, Type javaType)
        {
            this.catalog = catalog;
            this.CSharpType = javaType;
            this.ProtocolType = catalog.GetThriftProtocolType(javaType);
        }

        public Type CSharpType { get; }

        public ThriftProtocolType ProtocolType { get; }

        public bool Recursive
        {
            get { return true; }
        }

        public ThriftType Get()
        {
            ThriftType resolvedType = catalog.GetThriftTypeFromCache(this.CSharpType);
            if (resolvedType == null)
            {
                throw new NotSupportedException(
                    $"Attempted to resolve a recursive reference to type '{this.CSharpType.FullName}' before the referenced type was cached (most likely a recursive type support bug)");
            }
            return resolvedType;
        }

        public override String ToString()
        {
            if (this.IsResolved)
            {
                return "Resolved reference to " + Get();
            }
            else
            {
                return $"Unresolved reference to ThriftType for { this.CSharpType.FullName}";
            }
        }

        public override int GetHashCode()
        {
            return (this.catalog?.GetHashCode() ?? 0) + (this.CSharpType?.GetHashCode() ?? 0);
        }
        
        public override bool Equals(Object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null || !obj.GetType().Equals(typeof(RecursiveThriftTypeReference)))
            {
                return false;
            }

            RecursiveThriftTypeReference that = (RecursiveThriftTypeReference)obj;

            return Object.Equals(this.catalog, that.catalog) &&
                   Object.Equals(this.CSharpType, that.CSharpType);
        }

        private bool IsResolved
        {
            get { return catalog.GetThriftTypeFromCache(this.CSharpType) != null; }
        }
    }

}
