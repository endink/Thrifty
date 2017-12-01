using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public abstract class FieldMetadata
    {
        private FieldKind _type;
        private const String RecursiveReferenceAnnotationName = "swift.recursive_reference";

        protected FieldMetadata(ThriftFieldAttribute annotation, FieldKind type) : this(annotation, type, null)
        {
        }

        private FieldMetadata(ThriftFieldAttribute annotation, FieldKind type, IEnumerable<ThriftIdlAttribute> idlCollection)
        {
            this.Annotation = annotation;
            this.Id = short.MinValue;
            this._type = type;
            idlCollection = idlCollection ?? Enumerable.Empty<ThriftIdlAttribute>();
            switch (type)
            {
                case FieldKind.ThriftField:
                    if (annotation != null)
                    {
                        if (annotation.Id != short.MinValue)
                        {
                            this.Id = annotation.Id;
                        }
                        //isLegacyId = annotation.isLegacyId();
                        if (!String.IsNullOrWhiteSpace(annotation.Name))
                        {
                            this.Name = annotation.Name;
                        }
                        this.Requiredness = annotation.Required;

                        Dictionary<String, String> idlAnnotations = new Dictionary<String, String>();
                        foreach (var idlAnnotation in idlCollection)
                        {
                            idlAnnotations[idlAnnotation.Key] = idlAnnotation.Value;
                        }

                        if (annotation.Recursive != ThriftFieldAttribute.Recursiveness.Unspecified)
                        {
                            switch (annotation.Recursive)
                            {
                                case ThriftFieldAttribute.Recursiveness.True:
                                    this.IsRecursiveReference = true;
                                    break;
                                case ThriftFieldAttribute.Recursiveness.False:
                                    this.IsRecursiveReference = false;
                                    break;
                                default:
                                    throw new ArgumentException("Unexpected get for isRecursive field");
                            }
                        }
                        else if (idlAnnotations.ContainsKey(RecursiveReferenceAnnotationName))
                        {
                            String v = null;
                            idlAnnotations.TryGetValue(RecursiveReferenceAnnotationName, out v);
                            if (v == null || String.IsNullOrWhiteSpace(v))
                            {
                                v = "false";
                            }
                            this.IsRecursiveReference = (String.Compare(bool.TrueString, v, true) == 0);
                        }
                    }
                    break;
                case FieldKind.ThriftUnionId:
                    System.Diagnostics.Contracts.Contract.Assert(annotation == null, "ThriftStruct annotation shouldn't be present for THRIFT_UNION_ID");
                    this.Id = short.MinValue;
                    //isLegacyId = true; // preserve `negative field ID <=> isLegacyId`
                    this.Name = "_union_id";
                    break;
                default:
                    throw new ArgumentException($"Encountered field metadata type {type}.");
            }
        }

        internal ThriftFieldAttribute Annotation { get; }

        public short Id { get; set; }

        public ThriftFieldAttribute.Requiredness Requiredness { get; set; }

        public bool? IsRecursiveReference { get; internal set; }

        //public bool isLegacyId()
        //{
        //    return isLegacyId;
        //}

        //public void setIsLegacyId(Boolean isLegacyId)
        //{
        //    this.isLegacyId = isLegacyId;
        //}

        public string Name { get; set; }

        public IDictionary<String, String> IdlAnnotations { get; set; }

        public FieldKind Type
        {
            get { return _type; }
        }

        public abstract Type CSharpType { get; }

        public abstract String ExtractName();

        internal static Func<FieldMetadata, short?> GetThriftFieldId()
        {
            return input =>
            {
                if (input == null)
                {
                    return null;
                }
                short value = input.Id;
                return value;
            };
        }



        internal static Func<FieldMetadata, String> GetThriftFieldName()
        {
            return input =>
            {
                if (input == null)
                {
                    return null;
                }
                return input.Name;
            };
        }

        internal static Func<FieldMetadata, String> GetOrExtractThriftFieldName()
        {
            return input =>
            {
                if (input == null)
                {
                    return null;
                }
                String name = input.Name;
                if (name == null)
                {
                    name = input.ExtractName();
                }
                if (name == null)
                {
                    throw new ArgumentNullException("name is null");
                }
                return name;
            };
        }

        internal static Func<FieldMetadata, String> ExtractThriftFieldName()
        {
            return input =>
            {
                if (input == null)
                {
                    return null;
                }
                return input.ExtractName();
            };
        }

        internal  static Func<FieldMetadata, ThriftFieldAttribute.Requiredness> GetThriftFieldRequiredness()
        {
            return input =>
            {
                return input.Requiredness;
            };
        }
    }
}
