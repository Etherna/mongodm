//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;
using Etherna.MongODM.Core.Serialization.Serializers;
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class SchemaRegistry : FreezableConfig, ISchemaRegistry
    {
        // Fields.
        private readonly Dictionary<Type, ISchema> _schemas = new();

        private readonly Dictionary<Type, BsonElement> activeModelMapIdBsonElement = new();
        private readonly ConcurrentDictionary<Type, BsonClassMap> defaultClassMapsCache = new();
        private readonly Dictionary<MemberInfo, List<MemberDependency>> memberInfoToMemberMapsDictionary = new();
        private readonly Dictionary<Type, List<MemberDependency>> modelTypeToReferencedIdMemberMapsDictionary = new();

        private IDbContext dbContext = default!;

        // Constructor and initializer.
        public void Initialize(IDbContext dbContext)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");
            this.dbContext = dbContext;

            IsInitialized = true;
        }

        // Properties.
        public bool IsInitialized { get; private set; }
        public IReadOnlyDictionary<Type, ISchema> Schemas => _schemas;

        // Methods.
        public ICustomSerializerSchemaBuilder<TModel> AddCustomSerializerSchema<TModel>(
            IBsonSerializer<TModel> customSerializer) where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (customSerializer is null)
                    throw new ArgumentNullException(nameof(customSerializer));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new CustomSerializerSchema<TModel>(customSerializer);
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The new model map instance can't be disposed")]
        public IModelMapsSchemaBuilder<TModel> AddModelMapsSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) where TModel : class
        {
            // Create model map.
            var modelMap = new ModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap())),
                serializer: customSerializer ?? ModelMap.GetDefaultSerializer<TModel>(dbContext));

            return AddModelMapsSchema(modelMap);
        }

        public IModelMapsSchemaBuilder<TModel> AddModelMapsSchema<TModel>(
            ModelMap<TModel> activeModelMap)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelMap is null)
                    throw new ArgumentNullException(nameof(activeModelMap));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ModelMapsSchema<TModel>(activeModelMap, dbContext);
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                // If model maps schema uses proxy model, register a new one for proxy type.
                if (modelSchemaConfiguration.ProxyModelType != null)
                {
                    var proxyModelSchema = CreateNewDefaultModelMapsSchema(modelSchemaConfiguration.ProxyModelType);
                    _schemas.Add(modelSchemaConfiguration.ProxyModelType, proxyModelSchema);
                }

                return modelSchemaConfiguration;
            });

        public BsonClassMap GetActiveClassMap(Type modelType)
        {
            // If a schema is registered.
            if (_schemas.ContainsKey(modelType) &&
                _schemas[modelType] is IModelMapsSchema modelMapSchema)
                return modelMapSchema.ActiveMap.BsonClassMap;

            // If we don't have a model schema, look for a default classmap, or create it.
            if (defaultClassMapsCache.ContainsKey(modelType))
                return defaultClassMapsCache[modelType];

            var classMapDefinition = typeof(BsonClassMap<>);
            var classMapType = classMapDefinition.MakeGenericType(modelType);
            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);
            classMap.AutoMap();

            // Register classMap (if doesn't exist) with discriminator.
            defaultClassMapsCache.TryAdd(modelType, classMap);
            dbContext.DiscriminatorRegistry.AddDiscriminator(modelType, classMap.Discriminator);

            return classMap;
        }

        public BsonElement GetActiveModelMapIdBsonElement(Type modelType)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));

            Freeze(); //needed for initialization

            /*
             * Use of this cache dictionary avoids checks and creation of new bson elements
             * for each serialization.
             */
            return activeModelMapIdBsonElement[modelType];
        }

        public IEnumerable<MemberDependency> GetIdMemberDependenciesFromRootModel(Type modelType)
        {
            Freeze(); //needed for initialization

            if (modelTypeToReferencedIdMemberMapsDictionary.TryGetValue(modelType, out List<MemberDependency> dependencies))
                return dependencies;
            return Array.Empty<MemberDependency>();
        }

        public IEnumerable<MemberDependency> GetMemberDependenciesFromMemberInfo(MemberInfo memberInfo)
        {
            Freeze(); //needed for initialization

            foreach (var pair in memberInfoToMemberMapsDictionary)
                if (pair.Key.IsSameAs(memberInfo))
                    return pair.Value;

            return Array.Empty<MemberDependency>();
        }

        public IModelMapsSchema GetModelMapsSchema(Type modelType)
        {
            if (modelType is null)
                throw new ArgumentNullException(nameof(modelType));
            if (!_schemas.ContainsKey(modelType))
                throw new KeyNotFoundException(modelType.Name + " schema is not registered");

            var schema = _schemas[modelType];

            if (schema is not IModelMapsSchema modelMapSchema)
                throw new InvalidOperationException(modelType.Name + " schema is not a model maps schema");

            return modelMapSchema;
        }

        public override string ToString()
        {
            StringBuilder strBuilder = new();

            // Member dependencies.
            //memberInfoToMemberMapsDictionary
            strBuilder.AppendLine("Member dependencies:");
            foreach (var dependencies in from dependency in memberInfoToMemberMapsDictionary
                                         orderby $"{dependency.Key.DeclaringType.Name}.{dependency.Key.Name}"
                                         select dependency)
            {
                strBuilder.AppendLine($"{dependencies.Key.DeclaringType.Name}.{dependencies.Key.Name}");
                foreach (var dependency in dependencies.Value.OrderBy(d => d.FullPathToString()))
                    strBuilder.AppendLine($"  {dependency}");
            }
            strBuilder.AppendLine();

            //modelTypeToReferencedIdMemberMapsDictionary
            strBuilder.AppendLine("Models to referenced Ids:");
            foreach (var dependencies in from dependency in modelTypeToReferencedIdMemberMapsDictionary
                                         orderby $"{dependency.Key.Name}"
                                         select dependency)
            {
                strBuilder.AppendLine($"{dependencies.Key.Name}");
                foreach (var dependency in dependencies.Value.OrderBy(d => d.FullPathToString()))
                    strBuilder.AppendLine($"  {dependency}");
            }

            return strBuilder.ToString();
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            // Link model maps with their base map.
            LinkBaseModelMaps();

            // Freeze, register serializers and compile registers.
            foreach (var schema in _schemas.Values)
            {
                // Freeze schema.
                schema.Freeze();

                // Register active serializer.
                if (schema.ActiveSerializer != null)
                    ((BsonSerializerRegistry)dbContext.SerializerRegistry).RegisterSerializer(schema.ModelType, schema.ActiveSerializer);

                // Register discriminators for all bson class maps.
                if (schema is IModelMapsSchema modelMapsSchema)
                    foreach (var modelMap in modelMapsSchema.AllMapsDictionary.Values)
                        dbContext.DiscriminatorRegistry.AddDiscriminator(modelMapsSchema.ModelType, modelMap.BsonClassMap.Discriminator);
            }

            // Specific for model maps schemas.
            foreach (var schema in _schemas.Values.OfType<IModelMapsSchema>())
            {
                // Compile dependency registers.
                /*
                 * Only model map based schemas can be analyzed for document dependencies.
                 * Schemas based on custom serializers can't be explored.
                 * 
                 * This operation needs to be executed AFTER that all serializers have been registered.
                 */
                foreach (var modelMap in schema.AllMapsDictionary.Values)
                    CompileDependencyRegisters(
                        modelMap,
                        modelMap.BsonClassMap,
                        default,
                        Array.Empty<OwnedBsonMemberMap>());

                // Generate active model maps id bson elements.
                /*
                 * If current model type is proxy, we need to use id of its base type. This because
                 * when we serialize a proxy model, we don't want that in the proxy's model map id
                 * will be reported on document, but we want to serialize its original type's id.
                 */
                var notProxySchema = GetModelMapsSchema(dbContext.ProxyGenerator.PurgeProxyType(schema.ModelType));

                activeModelMapIdBsonElement.Add(
                    schema.ModelType,
                    new BsonElement(
                        dbContext.Options.ModelMapVersion.ElementName,
                        new BsonString(notProxySchema.ActiveMap.Id)));
            }
        }

        // Helpers.
        private void CompileDependencyRegisters(
            IModelMap modelMap,
            BsonClassMap currentClassMap,
            BsonClassMap? lastEntityClassMap,
            IEnumerable<OwnedBsonMemberMap> ownedBsonMemberPath,
            bool? useCascadeDeleteSetting = null)
        {
            // Ignore class maps of abstract types. (child classes will map all their members)
            if (currentClassMap.ClassType.IsAbstract)
                return;
            // Ignore class maps of proxy types. (they are not useful in dependency context)
            if (dbContext.ProxyGenerator.IsProxyType(currentClassMap.ClassType))
                return;

            // Identify last indented entity class maps.
            if (currentClassMap.IsEntity())
                lastEntityClassMap = currentClassMap;

            // Explore recursively members.
            foreach (var ownedBsonMemberMap in currentClassMap.AllMemberMaps.Select(member => new OwnedBsonMemberMap(currentClassMap, member)))
            {
                // Update path.
                var currentMemberPath = ownedBsonMemberPath.Append(ownedBsonMemberMap);

                // Identify current member with its root model map, the path from current model map, and cascade delete information.
                var memberDependency = new MemberDependency(
                    modelMap,
                    currentMemberPath,
                    useCascadeDeleteSetting ?? false);

                // Add member dependency to registers.

                //memberInfo to related member dependencies, for each different model maps version
                /*
                 * MemberInfo comparison has to be performed with extension method "IsSameAs". If an equal member info
                 * is found with this equality comparer, it has to be taken as key also for current memberinfo
                 */
                List<MemberDependency>? memberDependencyList = default;
                foreach (var pair in memberInfoToMemberMapsDictionary)
                    if (pair.Key.IsSameAs(ownedBsonMemberMap.Member.MemberInfo))
                    {
                        memberDependencyList = memberInfoToMemberMapsDictionary[pair.Key];
                        break;
                    }
                if (memberDependencyList is null)
                {
                    memberDependencyList = new List<MemberDependency>();
                    memberInfoToMemberMapsDictionary[ownedBsonMemberMap.Member.MemberInfo] = memberDependencyList;
                }

                memberDependencyList.Add(memberDependency);

                //model type to each referenced id member maps, for each different model maps version
                if (!modelTypeToReferencedIdMemberMapsDictionary.ContainsKey(modelMap.ModelType))
                    modelTypeToReferencedIdMemberMapsDictionary[modelMap.ModelType] = new List<MemberDependency>();

                if (memberDependency.IsEntityReferenceMember && memberDependency.IsIdMember)
                    modelTypeToReferencedIdMemberMapsDictionary[modelMap.ModelType].Add(memberDependency);

                // Analize recursion on member.
                var memberSerializer = ownedBsonMemberMap.Member.GetSerializer();

                //model maps schema serializers
                if (memberSerializer is IModelMapsContainerSerializer schemaSerializer)
                {
                    bool? useCascadeDelete = (memberSerializer as IReferenceContainerSerializer)?.UseCascadeDelete;
                    foreach (var childClassMap in schemaSerializer.AllChildClassMaps)
                        CompileDependencyRegisters(modelMap, childClassMap, lastEntityClassMap, currentMemberPath, useCascadeDelete);
                }
            }
        }

        private IModelMapsSchema CreateNewDefaultModelMapsSchema(Type modelType)
        {
            //class map
            var classMapDefinition = typeof(BsonClassMap<>);
            var classMapType = classMapDefinition.MakeGenericType(modelType);

            var classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

            //model map
            var modelMapDefinition = typeof(ModelMap<>);
            var modelMapType = modelMapDefinition.MakeGenericType(modelType);

            var activeModelMap = (ModelMap)Activator.CreateInstance(
                modelMapType,
                Guid.NewGuid().ToString(), //string id
                classMap,                  //BsonClassMap<TModel> bsonClassMap
                null,                      //string? baseModelMapId
                null,                      //Func<TModel, Task<TModel>>? fixDeserializedModelFunc
                null);                     //IBsonSerializer<TModel>? serializer

            //model maps schema
            var modelMapsSchemaDefinition = typeof(ModelMapsSchema<>);
            var modelMapsSchemaType = modelMapsSchemaDefinition.MakeGenericType(modelType);

            return (IModelMapsSchema)Activator.CreateInstance(
                modelMapsSchemaType,
                activeModelMap,      //ModelMap<TModel> activeMap
                dbContext);          //IDbContext dbContext
        }

        private void LinkBaseModelMaps()
        {
            /* A stack with a while iteration is needed, instead of a foreach construct,
             * because we will add new schemas if needed. Foreach is based on enumerable
             * iterator, and if an enumerable is modified during foreach execution, an
             * exception is rised.
             */
            var processingSchemas = new Stack<IModelMapsSchema>(_schemas.Values.OfType<IModelMapsSchema>());

            while (processingSchemas.Any())
            {
                var schema = processingSchemas.Pop();
                var baseModelType = schema.ModelType.BaseType;

                // If don't need to be linked, because it is typeof(object).
                if (baseModelType is null)
                    continue;

                // Get base type schema, or generate it.
                if (!_schemas.TryGetValue(baseModelType, out ISchema baseSchema))
                {
                    // Create schema instance.
                    baseSchema = CreateNewDefaultModelMapsSchema(baseModelType);

                    // Register schema instance.
                    _schemas.Add(baseModelType, baseSchema);
                    processingSchemas.Push((IModelMapsSchema)baseSchema);
                }

                // Process schema's model maps.
                foreach (var modelMap in schema.AllMapsDictionary.Values)
                {
                    // Search base model map.
                    var baseModelMap = modelMap.BaseModelMapId != null ?
                        ((IModelMapsSchema)baseSchema).AllMapsDictionary[modelMap.BaseModelMapId] :
                        ((IModelMapsSchema)baseSchema).ActiveMap;

                    // Link base model map.
                    modelMap.SetBaseModelMap(baseModelMap);
                }
            }
        }
    }
}
