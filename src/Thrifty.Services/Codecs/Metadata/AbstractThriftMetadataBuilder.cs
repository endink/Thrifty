using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Thrifty.Codecs.Metadata
{
    public abstract class AbstractThriftMetadataBuilder
    {
        private readonly List<FieldMetadata> _fields;
        // readers readonly 
        private readonly IList<Extractor> _extractors;

        // writers 
        private readonly IList<MethodInjection> _builderMethodInjections;
        private readonly IList<ConstructorInjection> _constructorInjections;


        private readonly IList<FieldInjection> _fieldInjections;
        private readonly IList<MethodInjection> _methodInjections;

        private readonly MetadataErrors _metadataErrors;

        protected AbstractThriftMetadataBuilder(ThriftCatalog catalog, Type structType)
        {
            Guard.ArgumentNotNull(catalog, nameof(catalog));
            Guard.ArgumentNotNull(structType, nameof(structType));

            this._fields = new List<FieldMetadata>();
            this._extractors = new List<Extractor>();
            this._builderMethodInjections = new List<MethodInjection>();
            this._constructorInjections = new List<ConstructorInjection>();
            this._fieldInjections = new List<FieldInjection>();
            this._methodInjections = new List<MethodInjection>();

            this.Catalog = catalog;
            this.StructType = structType;
            this._metadataErrors = new MetadataErrors(catalog.Monitor);

            // assign the struct name from the annotation or from the Java class
            this.StructName = ExtractName();
            // get the builder type from the annotation or from the Java class
            this.BuilderType = ExtractBuilderType();

            // extract all of the annotated constructor and report an error if
            // there is more than one or none

            // extract thrift fields from the annotated fields and verify
            ExtractFromFields();

            // also extract thrift fields from the annotated parameters and verify
            ExtractFromConstructors();
            
            //不考虑支持方法注入
            // extract thrift fields from the annotated methods (and parameters) and verify
            //extractFromMethods();
        }

        protected abstract Type ExtractBuilderClass();

        protected abstract String ExtractName();

        protected virtual IDictionary<String, String> ExtractStructIdlAnnotations()
        {
            return new Dictionary<String, String>(0);
        }

        protected virtual void ValidateConstructors(IEnumerable<ConstructorInjection> constuctors)
        {
            if (constuctors.Count() > 1)
            {
                _metadataErrors.AddError($"Multiple constructors are attributed with {nameof(ThriftConstructorAttribute)} on type '{this.StructType.FullName}'.");
            }
        }

        protected abstract ThriftFieldMetadata BuildField(IEnumerable<FieldMetadata> input);

        public abstract ThriftStructMetadata Build();

        public MetadataErrors MetadataErrors
        {
            get { return _metadataErrors; }
        }

        protected IEnumerable<ConstructorInjection> ConstructorInjections
        {
            get { return this._constructorInjections; }
        }

        protected ThriftCatalog Catalog { get; }

        public String StructName { get; }

        public Type StructType { get; }

        public Type BuilderType { get; }

        public List<FieldMetadata> Fields
        {
            get
            {
                return _fields;
            }
        }

        private Type ExtractBuilderType()
        {
            var type = this.ExtractBuilderClass();
            var builderType = type?.GetTypeInfo();
            if (builderType == null)
            {
                return null;
            }

            var structType = this.StructType.GetTypeInfo();

            if (!builderType.IsGenericType)
            {
                return BuilderType;
            }

            if (!(structType.IsGenericType))
            {
                _metadataErrors.AddError($"Builder type {builderType.Name} may only be generic if the type it builds ('{structType.Name}') is also generic.");
                return type;
            }

            if (builderType.GenericTypeParameters.Length != structType.GenericTypeParameters.Length)
            {
                _metadataErrors.AddError($"Generic builder class '{builderType.Name}' must have the same number of type parameters as the type it builds ('{structType.Name}').");
            }

            return type;
        }



        protected IEnumerable<ParameterInjection> GetParameterInjections(Type structType, ParameterInfo[] parametersList)
        {
            List<ParameterInjection> parameters = new List<ParameterInjection>(parametersList.Length);

            for (int parameterIndex = 0; parameterIndex < parametersList.Length; parameterIndex++)
            {
                var p = parametersList[parameterIndex];
                ThriftFieldAttribute thriftField = p.GetCustomAttribute<ThriftFieldAttribute>();
                /*
                  这里以属性上的 ThriftFieldAttribute 优先，否则将双写 ThriftFieldAttribute 上的各种属性，例如 Required, Name，这将非常麻烦。
                  或许，最合理的方案应该是定义一个 ThriftParameterAttribute，只给一个 id 属性 ？
                  JAVA 字段名和构造函数参数均是小驼峰，不存在此问题，但依然存在 Required 必须一致的问题，也很麻烦， 优先使用字段配置可以缓解这个问题。
                 */
                string extractedName = p.Name;
                if (thriftField != null) 
                {
                    var field = _fields.FirstOrDefault(f => f.Id == thriftField.Id);
                    thriftField = field?.Annotation ?? thriftField;
                    extractedName = field?.ExtractName() ?? p.Name;
                }
                 
                ParameterInjection parameterInjection = new ParameterInjection(
                        structType,
                        parameterIndex,
                        thriftField,
                        extractedName,
                        p.ParameterType
                );

                parameters.Add(parameterInjection);
            }
            return parameters;
        }


        protected void AddConstructors(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (ConstructorInfo constructor in constructors)
            {
                var ctorAttr = constructor.GetCustomAttribute<ThriftConstructorAttribute>(false);
                if (ctorAttr == null)
                {
                    continue;
                }

                if (!constructor.IsPublic)
                {
                    _metadataErrors.AddError($"ThriftConstructorAttribute '{constructor.Name}' is not public");
                    continue;
                }
                
                IEnumerable<ParameterInjection> parameters = GetParameterInjections(type, constructor.GetParameters());
                if (parameters != null)
                {
                    _fields.AddRange(parameters);
                    _constructorInjections.Add(new ConstructorInjection(constructor, parameters));
                }
            }

            // add the default constructor
            if (!_constructorInjections.Any())
            {
                var defaultCtor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                if (defaultCtor != null)
                {
                    if (!defaultCtor.IsPublic)
                    {
                        _metadataErrors.AddError($"Default constructor '{defaultCtor.Name}' is not public");
                    }
                    _constructorInjections.Add(new ConstructorInjection(defaultCtor));
                }
            }

            ValidateConstructors(this._constructorInjections);
        }


        protected void VerifyType<T>()
            where T: Attribute
        {
            Type attribute = typeof(T);
            var info = this.StructType.GetTypeInfo();
            // Verify struct class is public and final
            if (!info.IsPublic)
            {
                _metadataErrors.AddError($"{attribute.Name} class '{this.StructType.Name}' is not public");
            }

            if (info.GetCustomAttribute(attribute) == null)
            {
                _metadataErrors.AddError($"{attribute.Name} class '{this.StructType.Name}' does not have a {attribute.Name}");
            }
        }

        protected void ExtractFromConstructors()
        {
            if (this.BuilderType == null)
            {
                // struct class must have a valid constructor
                AddConstructors(this.StructType);
            }
            else
            {
                // builder class must have a valid constructor
                AddConstructors(this.BuilderType);

                // builder class must have a build method annotated with @ThriftConstructor
                AddBuilderMethods();

                // verify struct class does not have @ThriftConstructors
                foreach (ConstructorInfo constructor in this.StructType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (constructor.GetCustomAttribute(typeof(ThriftConstructorAttribute)) != null)
                    {
                        _metadataErrors.AddWarning($"Thrift class '{this.StructType}' has a builder class, but constructor attributed with {nameof(ThriftConstructorAttribute)}");
                    }
                }
            }
        }



        protected void ExtractFromFields()
        {
            AddFields(this.StructType);
        }

        public static MethodInfo FindAttributedMethod(Type configClass, Type annotation, String methodName, params Type[] paramTypes)
        {
            try
            {
                MethodInfo method = configClass.GetMethod(methodName, paramTypes);
                if (method != null && method.GetCustomAttribute(annotation) != null)
                {
                    return method;
                }
            }
            catch (MissingMethodException)
            {
                // ignore
            }
            //显示接口方法。
            foreach (Type iface in configClass.GetInterfaces())
            {
                MethodInfo managedMethod = FindAttributedMethod(iface, annotation, methodName, paramTypes);
                if (managedMethod != null)
                {
                    return managedMethod;
                }
            }

            return null;
        }

        public static IEnumerable<MethodInfo> FindAttributedMethods(Type type, Type attribute)
        {
            List<MethodInfo> result = new List<MethodInfo>();

            // gather all publicly available methods
            // this returns everything, even if it's declared in a parent
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // look for annotations recursively in super-classes or interfaces
                MethodInfo managedMethod = FindAttributedMethod(
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

        protected bool HasThriftFieldAttribute(MethodInfo method)
        {
            var parameters = method.GetParameters();
            foreach (var param in method.GetParameters())
            {
                if (param.GetCustomAttributes(typeof(ThriftFieldAttribute)).Any())
                {
                    return true;
                }
            }
            return false;
        }


        protected void AddBuilderMethods()
        {
            foreach (MethodInfo method in FindAttributedMethods(this.StructType, typeof(ThriftConstructorAttribute)))
            {
                var parameters = GetParameterInjections(this.StructType, method.GetParameters());

                // parameters are null if the method is misconfigured
                if (parameters != null)
                {
                    _fields.AddRange(parameters);
                    _builderMethodInjections.Add(new MethodInjection(method, parameters));
                }

                if (!this.StructType.IsAssignableFrom(method.ReturnType))
                {
                    _metadataErrors.AddError(
                            $@"'{this.StructType}' says that '{this.BuilderType}' is its builder class, but {typeof(ThriftConstructorAttribute).Name} method '{method.Name}' in the builder does not build an instance assignable to that type");
                }
            }

            // find invalid methods not skipped by findAnnotatedMethods()
            foreach (MethodInfo method in this.StructType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var methodAttr = method.GetCustomAttribute(typeof(ThriftConstructorAttribute));
                var hasField = HasThriftFieldAttribute(method);
                if (methodAttr != null || hasField)
                {
                    if (!method.IsPublic)
                    {
                        _metadataErrors.AddError($"{nameof(ThriftConstructorAttribute)} method '{method.Name}' is not public");
                    }
                    if (method.IsStatic)
                    {
                        _metadataErrors.AddError($"{nameof(ThriftConstructorAttribute)} method '{method.Name}' is static");
                    }
                }
            }

            if (!_builderMethodInjections.Any())
            {
                _metadataErrors.AddError($"Struct builder class '{BuilderType.Name}' does not have a public builder method annotated with {nameof(ThriftConstructorAttribute)}.");
            }
            if (_builderMethodInjections.Count > 1)
            {
                _metadataErrors.AddError($"Multiple builder methods are annotated with {nameof(ThriftConstructorAttribute)}");
            }
        }

        protected void AddFields(Type type)
        {
            var properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttributes(typeof(ThriftFieldAttribute)).Any()).ToArray();
            foreach (var protperty in properties)
            {
                AddField(protperty);
            }

            // 验证一下用法错误的 attribute
            properties = type.GetProperties(
                   BindingFlags.Static | BindingFlags.DeclaredOnly)
                  .Where(p => p.GetCustomAttributes(typeof(ThriftFieldAttribute)).Any()).ToArray();

            foreach (var p in properties)
            {
                _metadataErrors.AddError($"{nameof(ThriftFieldAttribute)} property '{p.DeclaringType.Name}.{p.Name}' is nopublic or static.");
            }
        }

        protected void AddField(PropertyInfo fieldField)
        {
            var att = fieldField.GetCustomAttribute<ThriftFieldAttribute>();
            if (att == null)
            {
                throw new ThriftyException($"{fieldField.DeclaringType}.{fieldField.Name} has no {nameof(ThriftFieldAttribute)}");
            }
            if (fieldField.CanRead)
            {
                FieldExtractor fieldExtractor = new FieldExtractor(fieldField, att, FieldKind.ThriftField);
                _fields.Add(fieldExtractor);
                _extractors.Add(fieldExtractor);
            }
            if (fieldField.CanWrite)
            {
                FieldInjection fieldInjection = new FieldInjection(fieldField, att, FieldKind.ThriftField);
                _fields.Add(fieldInjection);
                _fieldInjections.Add(fieldInjection);
            }
        }
        
        protected void NormalizeThriftFields(ThriftCatalog catalog)
        {
            // assign all fields an id (if possible)
            var fieldsWithConflictingIds = InferThriftFieldIds();

            // group fields by id
            var fieldsById = _fields.GroupBy(f=>f.Id);
            foreach (var entry in fieldsById)
            {
                var fields = entry;
                //fields must have an id
                if (entry.Key == short.MinValue)
                {
                    var names = fields.Select(FieldMetadata.GetOrExtractThriftFieldName());
                    foreach (String n in names)
                    {
                        // only report errors for fields that don't have conflicting ids
                        if (!fieldsWithConflictingIds.Contains(n))
                        {
                            this.MetadataErrors.AddError($"Thrift class '{StructName}' fields {String.Join(", ", names)} do not have an id.");
                        }
                    }
                    continue;
                }

                short fieldId = entry.Key;

                // ensure all fields for this ID have the same name
                String fieldName = ExtractFieldName(fieldId, fields);
                foreach (FieldMetadata field in fields)
                {
                    field.Name = fieldName;
                }

                // ensure all fields for this ID have the same requiredness
                ThriftFieldAttribute.Requiredness requiredness = ExtractFieldRequiredness(fieldId, fieldName, fields);
                foreach (FieldMetadata field in fields)
                {
                    if (field.Requiredness != ThriftFieldAttribute.Requiredness.Unspecified)
                    {
                        field.Requiredness = requiredness;
                    }
                    else
                    {
                        field.Requiredness = (catalog.IsNullable(field.CSharpType)) ? ThriftFieldAttribute.Requiredness.Optional : ThriftFieldAttribute.Requiredness.Required;
                    }
                }

                // We need to do the isLegacyId check in two places. We've already done this
                // process for fields which had multiple `@ThriftField` annotations when we
                // assigned them all the same ID. It doesn't hurt to do it again. On the other
                // hand, we need to do it now to catch the fields which only had a single
                // @ThriftAnnotation, because inferThriftFieldIds skipped them.
                //boolean isLegacyId = extractFieldIsLegacyId(fieldId, fieldName, fields);
                //for (FieldMetadata field : fields)
                //{
                //    field.setIsLegacyId(isLegacyId);
                //}

                var idlAnnotations = ExtractFieldIdlAnnotations(fieldId, fields);
                foreach (FieldMetadata field in fields)
                {
                    field.IdlAnnotations = idlAnnotations;
                }

                // ensure all fields for this ID have the same non-null get for isRecursiveReference
                bool isRecursiveReference = ExtractFieldIsRecursiveReference(fieldId, fields);
                foreach (FieldMetadata field in fields)
                {
                    field.IsRecursiveReference = isRecursiveReference;
                }

                // verify fields have a supported java type and all fields
                // for this ID have the same thrift type
                VerifyFieldType(fieldId, fieldName, fields, catalog);
            }
        }

        ///<summary>
        ///Assigns all fields an id if possible.  Fields are grouped by name and for each group, if there
        ///is a single id, all fields in the group are assigned this id.  If the group has multiple ids,
        ///an error is reported.
        ///</summary>
        protected ISet<String> InferThriftFieldIds()
        {
            ISet<String> fieldsWithConflictingIds = new HashSet<String>();

            // group fields by explicit name or by name extracted from field, method or property
            var fieldsByExplicitOrExtractedName = this._fields.GroupBy(FieldMetadata.GetOrExtractThriftFieldName());

            InferThriftFieldIds(fieldsByExplicitOrExtractedName, fieldsWithConflictingIds);

            // group fields by name extracted from field, method or property
            // this allows thrift name to be set explicitly without having to duplicate the name on getters and setters
            // todo should this be the only way this works?
            var fieldsByExtractedName = _fields.GroupBy(FieldMetadata.ExtractThriftFieldName());
            InferThriftFieldIds(fieldsByExtractedName, fieldsWithConflictingIds);

            return fieldsWithConflictingIds;
        }

        protected void InferThriftFieldIds(IEnumerable<IGrouping<String, FieldMetadata>> fieldsByName,
            ISet<String> fieldsWithConflictingIds)
        {
            // for each name group, set the ids on the fields without ids
            foreach (var entry in fieldsByName)
            {
                var fields = entry;
                var fieldName = entry.Key;

                // skip all entries without a name or singleton groups... we'll deal with these later
                if (fields.Count() <= 1)
                {
                    continue;
                }
                // all ids used by this named field
                var ids = fields.Select(f=>f.Id).Where(id=>id != short.MinValue).Distinct();

                // multiple conflicting ids
                if (ids.Count() > 1)
                {
                    if (!fieldsWithConflictingIds.Contains(fieldName))
                    {
                        _metadataErrors.AddError($"Thrift class '{this.StructName}' field '{fieldName}' has multiple ids: {String.Join(", ", ids)}.");
                        fieldsWithConflictingIds.Add(fieldName);
                    }
                    continue;
                }

                // single id, so set on all fields in this group (groups with no id are handled later),
                // and validate isLegacyId is consistent and correct.
                if (ids.Count() == 1)
                {
                    short id = ids.Single();

                    // propagate the id data to all fields in this group
                    foreach (FieldMetadata field in fields)
                    {
                        field.Id = id;
                    }
                }
            }
        }

        protected IDictionary<String, String> ExtractFieldIdlAnnotations(short fieldId, IEnumerable<FieldMetadata> fields)
        {
            var idlAnnotationMaps = fields.Where(f => f != null && f.IdlAnnotations != null && f.IdlAnnotations.Any())
                .Select(f => f.IdlAnnotations).Distinct();


            if (idlAnnotationMaps.Count() > 1)
            {
                _metadataErrors.AddError($"Thrift class '{this.StructName}' field '{fieldId}' has conflicting IDL annotation maps");
            }
            return idlAnnotationMaps.FirstOrDefault() ?? new Dictionary<String, String>();
        }

        protected bool ExtractFieldIsRecursiveReference(short fieldId, IEnumerable<FieldMetadata> fields)
        {
            var references = fields.Where(f=>f.IsRecursiveReference.HasValue).Select(f => f.IsRecursiveReference.Value).Distinct().ToArray();

            if (references.Length == 0)
            {
                return false;
            }

            if (references.Length > 1)
            {
                _metadataErrors.AddError($"Thrift class '{this.StructName}' field '{fieldId}' has both IsRecursiveReference=true and IsRecursiveReference=false");
            }
            return references.First();
        }

        protected String ExtractFieldName(short id, IEnumerable<FieldMetadata> fields)
        {

            // get the names used by these fields
            var names = fields.Select(FieldMetadata.ExtractThriftFieldName()).Where(n => !String.IsNullOrWhiteSpace(n)).Distinct();

            String name;
            if (names.Any())
            {
                if (names.Count() > 1)
                {
                    _metadataErrors.AddWarning($"Thrift class {this.StructName} field {id} has multiple names {String.Join(", ", names)}");
                }
                name = names.First();
            }
            else
            {
                throw new ThriftyException("cant get name from FieldMetadata collection");
            }
            return name;
        }

        protected ThriftFieldAttribute.Requiredness ExtractFieldRequiredness(short fieldId, String fieldName, IEnumerable<FieldMetadata> fields)
        {
            Func<ThriftFieldAttribute.Requiredness, bool> specificRequiredness
                        = input => (input != ThriftFieldAttribute.Requiredness.Unspecified);

            var requirednessValues = fields
                    .Select(FieldMetadata.GetThriftFieldRequiredness())
                    .Where(specificRequiredness).Distinct().ToArray();

            if (requirednessValues.Length > 1)
            {
                _metadataErrors.AddError($"Thrift class '{this.StructName}' field '{fieldName}({fieldId})' has multiple requiredness values: {String.Join(", ", requirednessValues)}");
            }

            ThriftFieldAttribute.Requiredness resolvedRequiredness;
            if (requirednessValues.Length == 0)
            {
                resolvedRequiredness = ThriftFieldAttribute.Requiredness.None;
            }
            else
            {
                resolvedRequiredness = requirednessValues.First();
            }

            return resolvedRequiredness;
        }

        ///<summary>
        ///Verifies that the the fields all have a supported Java type and that all fields map to the
        ///exact same ThriftType.
        ///</summary>
        protected void VerifyFieldType(short id, String name, IEnumerable<FieldMetadata> fields, ThriftCatalog catalog)
        {
            bool isSupportedType = true;
            foreach (FieldMetadata field in fields)
            {
                if (!catalog.IsSupportedStructFieldType(field.CSharpType))
                {
                    _metadataErrors.AddError($"Thrift class '{this.StructName}' field '{name}({id})' type '{field.CSharpType}' is not a supported C# type.");
                    isSupportedType = false;
                    // only report the error once
                    break;
                }
            }

            // fields must have the same type
            if (isSupportedType)
            {
                HashSet<IThriftTypeReference> types = new HashSet<IThriftTypeReference>();
                foreach (FieldMetadata field in fields)
                {
                    types.Add(catalog.GetFieldThriftTypeReference(field));
                }
                if (types.Count > 1)
                {
                    _metadataErrors.AddError($"Thrift class '{this.StructName}' field '{name}({id})' has multiple types: {String.Join(", ", types)}");
                }
            }
        }


        protected ThriftMethodInjection BuildBuilderConstructorInjections()
        {
            ThriftMethodInjection builderMethodInjection = null;
            if (this.BuilderType != null)
            {
                MethodInjection builderMethod = _builderMethodInjections.First();
                builderMethodInjection = new ThriftMethodInjection(builderMethod.Method, BuildParameterInjections(builderMethod.getParameters()));
            }
            return builderMethodInjection;
        }

        protected IEnumerable<ThriftFieldMetadata> BuildFieldInjections()
        {
            var fieldsById = _fields.GroupBy(f => f.Id);
            return fieldsById.Select(g =>
            {
                if (!g.Any())
                {
                    throw new ArgumentException("input is empty");
                }
                return this.BuildField(g);
            }).ToArray();
        }

        protected IEnumerable<ThriftMethodInjection> BuildMethodInjections()
        {
            return _methodInjections.Select(injection =>
            {
                return new ThriftMethodInjection(injection.Method, BuildParameterInjections(injection.getParameters()));
            }).ToArray();
        }

        protected IEnumerable<ThriftParameterInjection> BuildParameterInjections(IEnumerable<ParameterInjection> parameters)
        {
            return parameters.Select(injection =>
            {
                return new ThriftParameterInjection(
                injection.Id,
                injection.Name,
                injection.ParameterIndex,
                injection.CSharpType);
            }).ToArray();
        }
    }

}
