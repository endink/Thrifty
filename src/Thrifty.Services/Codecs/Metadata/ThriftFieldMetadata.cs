using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Thrifty.ThriftFieldAttribute;

namespace Thrifty.Codecs.Metadata
{
    public class ThriftFieldMetadata
    {
        private IThriftTypeReference _thriftTypeReference;
        private FieldKind _fieldKind;
        //private IDictionary<String, String> idlAnnotations;
        private ThriftConstructorInjection _constructorInjection;
        private ThriftMethodInjection _methodInjection;
        private IThriftExtraction _extraction;
        private TypeCoercion _coercion;
        //private IEnumerable<String> documentation;

        public ThriftFieldMetadata(
                short id,
                bool isRecursiveReference,
                Requiredness requiredness,
                //IEnumerable<KeyValuePair<String, String>> idlAnnotations,
                IThriftTypeReference thriftTypeReference,
                String name,
                FieldKind fieldKind,
                IEnumerable<IThriftInjection> injections = null,
                ThriftConstructorInjection constructorInjection = null,
                ThriftMethodInjection methodInjection = null,
                IThriftExtraction extraction = null,
                TypeCoercion coercion = null
        )
        {
            Guard.ArgumentNotNull(thriftTypeReference, nameof(thriftTypeReference));
            Guard.ArgumentNotNull(fieldKind, nameof(fieldKind));
            Guard.ArgumentNullOrWhiteSpaceString(name, nameof(name));
            Guard.ArgumentNotNull(thriftTypeReference, nameof(thriftTypeReference));
            Guard.ArgumentNullOrWhiteSpaceString(name, nameof(name));
            //Guard.ArgumentNotNull(constructorInjection, nameof(constructorInjection));
            //Guard.ArgumentNotNull(methodInjection, nameof(methodInjection));
            //Guard.ArgumentNotNull(extraction, nameof(extraction));
            //Guard.ArgumentNotNull(coercion, nameof(coercion));

            this.IsRecursiveReference = isRecursiveReference;
            this.Required = requiredness;
            this._thriftTypeReference = thriftTypeReference;
            this._fieldKind = fieldKind;
            this.Name = name;
            this.Injections = injections ?? Enumerable.Empty<IThriftInjection>();
            this._constructorInjection = constructorInjection;
            this._methodInjection = methodInjection;

            this._extraction = extraction;
            this._coercion = coercion;

            switch (fieldKind)
            {
                case FieldKind.ThriftField:
                    Guard.ArgumentCondition(id >= 0, "isLegacyId must be specified on fields with negative IDs", nameof(id));

                    break;
                case FieldKind.ThriftUnionId:
                    Guard.ArgumentCondition(id == short.MinValue, "thrift union id must be short.MinValue", nameof(id));
                    break;
            }

            Guard.ArgumentCondition(injections.Any()
                         || extraction != null
                         || constructorInjection != null
                         || methodInjection != null, "A thrift field must have an injection or extraction point");

            this.Id = id;

            if (extraction != null)
            {
                if (extraction is ThriftFieldExtractor)
                {
                    ThriftFieldExtractor e = (ThriftFieldExtractor)extraction;
                    //this.documentation = ThriftCatalog.getThriftDocumentation(e.getField());
                }
                else if (extraction != null && extraction is ThriftMethodExtractor)
                {
                    ThriftMethodExtractor e = (ThriftMethodExtractor)extraction;
                    //this.documentation = ThriftCatalog.getThriftDocumentation(e.getMethod());
                }
            }

            //this.idlAnnotations = idlAnnotations;
        }

        public bool TypeReferenceRecursive
        {
            get { return this._thriftTypeReference.Recursive; }
        }

        public bool IsRecursiveReference { get; }

        public String Name { get; }

        public IEnumerable<IThriftInjection> Injections { get; }

        public Requiredness Required { get; }

        public ThriftType ThriftType
        {
            get { return _thriftTypeReference.Get(); }
        }

        public FieldKind Type
        {
            get { return this._fieldKind; }
        }

        public bool ReadOnly
        {
            get
            {
                return !Injections.Any() && _constructorInjection == null && _methodInjection == null;
            }
        }

        public bool WriteOnly
        {
            get { return this._extraction == null; }
        }

        public short Id { get; }


        public bool TryGetExtraction(out IThriftExtraction extractor)
        {
            extractor = this._extraction;

            return extractor != null;
        }
    }
}
