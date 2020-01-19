using Digicando.MongODM.Models;
using Digicando.MongODM.Serialization.Modifiers;
using Digicando.MongODM.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Digicando.MongODM.Serialization
{
    class DocumentSchemaRegister : IDocumentSchemaRegister
    {
        // Fields.
        private readonly ReaderWriterLockSlim configLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly Dictionary<MemberInfo, List<DocumentSchemaMemberMap>> memberDependenciesMap =
            new Dictionary<MemberInfo, List<DocumentSchemaMemberMap>>();
        private readonly Dictionary<Type, List<DocumentSchemaMemberMap>> modelDependenciesMap =
            new Dictionary<Type, List<DocumentSchemaMemberMap>>();
        private readonly Dictionary<Type, List<DocumentSchemaMemberMap>> modelEntityReferencesIdsMap =
            new Dictionary<Type, List<DocumentSchemaMemberMap>>();

        private IDbContext dbContext;
        private readonly ISerializerModifierAccessor serializerModifierAccessor;
        private readonly List<DocumentSchema> schemas = new List<DocumentSchema>();

        // Constructors and initialization.
        public DocumentSchemaRegister(
            ISerializerModifierAccessor serializerModifierAccessor)
        {
            this.serializerModifierAccessor = serializerModifierAccessor;
        }

        public void Initialize(IDbContext dbContext)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");
            this.dbContext = dbContext;

            IsInitialized = true;
        }

        // Properties.
        public bool IsFrozen { get; private set; }
        public bool IsInitialized { get; private set; }
        public IEnumerable<DocumentSchema> Schemas => schemas;

        // Methods.
        public void Freeze()
        {
            configLock.EnterReadLock();
            try
            {
                if (IsFrozen) return;
            }
            finally
            {
                configLock.ExitReadLock();
            }

            configLock.EnterWriteLock();
            try
            {
                if (!IsFrozen)
                {
                    // Register class maps and serializers.
                    foreach (var schemaGroup in schemas.GroupBy(s => s.ModelType)
                                                       .Select(group => group.OrderByDescending(s => s.Version).First()))
                    {
                        // Register class map.
                        BsonClassMap.RegisterClassMap(schemaGroup.ClassMap);

                        // Register serializer.
                        if (schemaGroup.Serializer != null)
                            BsonSerializer.RegisterSerializer(schemaGroup.ModelType, schemaGroup.Serializer);
                    }

                    // Compile dependency registers.
                    foreach (var schema in schemas)
                    {
                        schema.ClassMap.Freeze();

                        CompileDependencyRegisters(
                            schema.ModelType,
                            new EntityMember[0],
                            schema.ClassMap,
                            schema.Version);
                    }

                    // Freeze.
                    IsFrozen = true;
                }
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        public IEnumerable<DocumentSchemaMemberMap> GetMemberDependencies(MemberInfo memberInfo)
        {
            Freeze();

            if (memberDependenciesMap.TryGetValue(memberInfo, out List<DocumentSchemaMemberMap> dependencies))
                return dependencies;
            return new DocumentSchemaMemberMap[0];
        }

        public IEnumerable<DocumentSchemaMemberMap> GetModelDependencies(Type modelType)
        {
            Freeze();

            if (modelDependenciesMap.TryGetValue(modelType, out List<DocumentSchemaMemberMap> dependencies))
                return dependencies;
            return new DocumentSchemaMemberMap[0];
        }

        public IEnumerable<DocumentSchemaMemberMap> GetModelEntityReferencesIds(Type modelType)
        {
            Freeze();

            if (modelEntityReferencesIdsMap.TryGetValue(modelType, out List<DocumentSchemaMemberMap> dependencies))
                return dependencies;
            return new DocumentSchemaMemberMap[0];
        }

        public void RegisterModelSchema<TModel>(
            DocumentVersion fromVersion,
            Func<IBsonSerializer<TModel>> initCustomSerializer = null,
            Func<TModel, DocumentVersion, IDbContext, Task<TModel>> modelMigrationAsync = null)
            where TModel : class =>
            RegisterModelSchema(
                fromVersion,
                new BsonClassMap<TModel>(cm => cm.AutoMap()),
                initCustomSerializer,
                modelMigrationAsync);

        public void RegisterModelSchema<TModel>(
            DocumentVersion fromVersion,
            Action<BsonClassMap<TModel>, ISerializerModifierAccessor> classMapInitializer,
            Func<IBsonSerializer<TModel>> initCustomSerializer = null,
            Func<TModel, DocumentVersion, IDbContext, Task<TModel>> modelMigrationAsync = null)
            where TModel : class =>
            RegisterModelSchema(
                fromVersion,
                new BsonClassMap<TModel>(cm => classMapInitializer(cm, serializerModifierAccessor)),
                initCustomSerializer,
                modelMigrationAsync);

        public void RegisterModelSchema<TModel>(
            DocumentVersion fromVersion,
            BsonClassMap<TModel> classMap,
            Func<IBsonSerializer<TModel>> initCustomSerializer = null,
            Func<TModel, DocumentVersion, IDbContext, Task<TModel>> modelMigrationAsync = null)
            where TModel : class
        {
            configLock.EnterWriteLock();
            try
            {
                if (IsFrozen)
                    throw new InvalidOperationException("Register is frozen");

                // Generate model serializer.
                IBsonSerializer<TModel> serializer = null;

                if (initCustomSerializer != null) //if custom is setted, keep it
                    serializer = initCustomSerializer();

                else if (!typeof(TModel).IsAbstract) //else if can deserialize, set default serializer
                    serializer =
                        new ExtendedClassMapSerializer<TModel>(
                            dbContext.DBCache,
                            dbContext.DocumentVersion,
                            serializerModifierAccessor,
                            (m, v) => modelMigrationAsync?.Invoke(
                                m, v, dbContext) ?? Task.FromResult(m))
                        { AddVersion = typeof(IEntityModel).IsAssignableFrom(typeof(TModel)) }; //true only for entity models

                // Register schema.
                schemas.Add(new DocumentSchema(classMap, typeof(TModel), serializer, fromVersion));
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        // Helpers.
        private void CompileDependencyRegisters(
            Type modelType,
            IEnumerable<EntityMember> memberPath,
            BsonClassMap currentClassMap,
            DocumentVersion version,
            bool? useCascadeDeleteSetting = null)
        {
            // Ignore class maps of abstract types. (child classes will map all their members)
            if (currentClassMap.ClassType.IsAbstract)
                return;

            // Identify last indented entity class maps.
            var lastEntityClassMap = currentClassMap.IdMemberMap != null ?
                currentClassMap :
                memberPath.LastOrDefault()?.EntityClassMap;

            // Explore recursively members.
            foreach (var memberMap in currentClassMap.AllMemberMaps)
            {
                // Update path.
                var newMemberPath = memberPath.Append(
                    new EntityMember(
                        lastEntityClassMap,
                        memberMap));

                // Add dependency to registers.
                var dependency = new DocumentSchemaMemberMap(
                    modelType,
                    newMemberPath,
                    version,
                    useCascadeDeleteSetting);

                if (!memberDependenciesMap.ContainsKey(memberMap.MemberInfo))
                    memberDependenciesMap[memberMap.MemberInfo] = new List<DocumentSchemaMemberMap>();
                if (!modelDependenciesMap.ContainsKey(modelType))
                    modelDependenciesMap[modelType] = new List<DocumentSchemaMemberMap>();
                if (!modelEntityReferencesIdsMap.ContainsKey(modelType))
                    modelEntityReferencesIdsMap[modelType] = new List<DocumentSchemaMemberMap>();

                memberDependenciesMap[memberMap.MemberInfo].Add(dependency);
                modelDependenciesMap[modelType].Add(dependency);
                if (dependency.IsEntityReferenceMember && dependency.IsIdMember)
                    modelEntityReferencesIdsMap[modelType].Add(dependency);

                // Analize recursion on member.
                var memberSerializer = memberMap.GetSerializer();
                var serializerType = memberSerializer.GetType();
                
                //custom serializers
                if (memberSerializer is IClassMapContainerSerializer classMapContainer)
                {
                    var useCascadeDelete = (memberSerializer as IReferenceContainerSerializer)?.UseCascadeDelete;
                    foreach (var childClassMap in classMapContainer.ContainedClassMaps)
                        CompileDependencyRegisters(modelType, newMemberPath, childClassMap, version, useCascadeDelete);
                }

                //default serializers
                else if (serializerType.IsGenericType &&
                    serializerType.GetGenericTypeDefinition() == typeof(BsonClassMapSerializer<>)) //default classmapp
                {
                    var memberClassMap = BsonClassMap.LookupClassMap(memberMap.MemberType);
                    CompileDependencyRegisters(modelType, newMemberPath, memberClassMap, version);
                }

                else if (serializerType.IsGenericType &&
                    serializerType.GetGenericTypeDefinition() == typeof(ImpliedImplementationInterfaceSerializer<,>)) //array
                {
                    var interfaceType = serializerType.GenericTypeArguments[0];
                    if (interfaceType.IsGenericType &&
                        (interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                         interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                    {
                        var elementType = interfaceType.GenericTypeArguments.Last();

                        var elementClassMap = BsonClassMap.LookupClassMap(elementType);
                        CompileDependencyRegisters(modelType, newMemberPath, elementClassMap, version);
                    }
                }
            }
        }

        private string MembersDependenciesToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (var dependencies in from dependency in memberDependenciesMap
                                         orderby $"{dependency.Key.DeclaringType.Name}.{dependency.Key.Name}"
                                         select dependency)
            {
                strBuilder.AppendLine($"{dependencies.Key.DeclaringType.Name}.{dependencies.Key.Name}");
                foreach (var dependency in dependencies.Value)
                    strBuilder.AppendLine($"  {dependency.ToString()}");
            }
            return strBuilder.ToString();
        }

        private string ModelDependenciesToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            foreach (var dependencies in from dependency in modelDependenciesMap
                                         orderby $"{dependency.Key.Name}"
                                         select dependency)
            {
                strBuilder.AppendLine($"{dependencies.Key.Name}");
                foreach (var dependency in dependencies.Value)
                    strBuilder.AppendLine($"  {dependency.ToString()}");
            }
            return strBuilder.ToString();
        }
    }
}
