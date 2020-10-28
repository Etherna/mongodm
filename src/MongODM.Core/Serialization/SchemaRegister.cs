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

using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Etherna.MongODM.Core.Serialization
{
    public class SchemaRegister : FreezableConfig, ISchemaRegister
    {
        // Fields.
        private readonly Dictionary<MemberInfo, List<ModelSchemaMemberMap>> memberDependenciesMap =
            new Dictionary<MemberInfo, List<ModelSchemaMemberMap>>();
        private readonly Dictionary<Type, List<ModelSchemaMemberMap>> modelDependenciesMap =
            new Dictionary<Type, List<ModelSchemaMemberMap>>();
        private readonly Dictionary<Type, List<ModelSchemaMemberMap>> modelEntityReferencesIdsMap =
            new Dictionary<Type, List<ModelSchemaMemberMap>>();

        private IDbContext dbContext = default!;
        private readonly ISerializerModifierAccessor serializerModifierAccessor;
        private readonly Dictionary<Type, ISchemaConfig> schemaConfigs = new Dictionary<Type, ISchemaConfig>();

        // Constructor, initialization.
        public SchemaRegister(
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
        public bool IsInitialized { get; private set; }

        // Methods.
        public ICustomSerializerSchemaConfig<TModel> AddCustomSerializerSchema<TModel>(
            IBsonSerializer<TModel> customSerializer,
            bool requireCollectionMigration = false) where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (customSerializer is null)
                    throw new ArgumentNullException(nameof(customSerializer));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new CustomSerializerSchemaConfig<TModel>(customSerializer, requireCollectionMigration);
                schemaConfigs.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        public IModelMapSchemaConfig<TModel> AddModelMapSchema<TModel>(
            string id,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null,
            bool requireCollectionMigration = false) where TModel : class =>
            AddModelMapSchema(new ModelMapSchema<TModel>(
                id,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap())),
                customSerializer), requireCollectionMigration);

        public IModelMapSchemaConfig<TModel> AddModelMapSchema<TModel>(
            ModelMapSchema<TModel> activeModelSchema,
            bool requireCollectionMigration = false) where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelSchema is null)
                    throw new ArgumentNullException(nameof(activeModelSchema));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ModelMapSchemaConfig<TModel>(
                    activeModelSchema, dbContext, requireCollectionMigration);
                schemaConfigs.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        public IEnumerable<ModelSchemaMemberMap> GetMemberDependencies(MemberInfo memberInfo)
        {
            Freeze();

            if (memberDependenciesMap.TryGetValue(memberInfo, out List<ModelSchemaMemberMap> dependencies))
                return dependencies;
            return Array.Empty<ModelSchemaMemberMap>();
        }

        public IEnumerable<ModelSchemaMemberMap> GetModelDependencies(Type modelType)
        {
            Freeze();

            if (modelDependenciesMap.TryGetValue(modelType, out List<ModelSchemaMemberMap> dependencies))
                return dependencies;
            return Array.Empty<ModelSchemaMemberMap>();
        }

        public IEnumerable<ModelSchemaMemberMap> GetModelEntityReferencesIds(Type modelType)
        {
            Freeze();

            if (modelEntityReferencesIdsMap.TryGetValue(modelType, out List<ModelSchemaMemberMap> dependencies))
                return dependencies;
            return Array.Empty<ModelSchemaMemberMap>();
        }

        // Protected methods.
        protected override void FreezeAction()
        {
            foreach (var schemaConfig in schemaConfigs.Values)
            {
                // Freeze schema config.
                schemaConfig.Freeze();

                // Register active serializers.
                if (schemaConfig.ActiveSerializer != null)
                {
                    //regular model
                    BsonSerializer.RegisterSerializer(schemaConfig.ModelType, schemaConfig.ActiveSerializer);

                    //proxy model
                    if (schemaConfig.ProxyModelType != null)
                        BsonSerializer.RegisterSerializer(schemaConfig.ProxyModelType, schemaConfig.ActiveSerializer);
                }

                // Compile dependency registers.
                /* Only model map based schemas can be analyzed for document dependencies.
                 * Schemas based on custom serializers can't be explored.
                 */
                if (schemaConfig is IModelMapSchemaConfig modelMapSchemaConfig)
                    foreach (var schema in modelMapSchemaConfig.SchemaDictionary.Values)
                        CompileDependencyRegisters(
                            schema.Id,
                            schema.ModelType,
                            schema.ModelMap,
                            Array.Empty<EntityMember>());
            }
        }

        // Helpers.
        private void CompileDependencyRegisters(
            string schemaId,
            Type modelType,
            BsonClassMap currentClassMap,
            IEnumerable<EntityMember> memberPath,
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
                var dependency = new ModelSchemaMemberMap(
                    modelType,
                    newMemberPath,
                    version,
                    useCascadeDeleteSetting);

                if (!memberDependenciesMap.ContainsKey(memberMap.MemberInfo))
                    memberDependenciesMap[memberMap.MemberInfo] = new List<ModelSchemaMemberMap>();
                if (!modelDependenciesMap.ContainsKey(modelType))
                    modelDependenciesMap[modelType] = new List<ModelSchemaMemberMap>();
                if (!modelEntityReferencesIdsMap.ContainsKey(modelType))
                    modelEntityReferencesIdsMap[modelType] = new List<ModelSchemaMemberMap>();

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
                        CompileDependencyRegisters(version, modelType, childClassMap, newMemberPath, useCascadeDelete);
                }

                //default serializers
                else if (serializerType.IsGenericType &&
                    serializerType.GetGenericTypeDefinition() == typeof(BsonClassMapSerializer<>)) //default classmapp
                {
                    var memberClassMap = BsonClassMap.LookupClassMap(memberMap.MemberType);
                    CompileDependencyRegisters(version, modelType, memberClassMap, newMemberPath);
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
                        CompileDependencyRegisters(version, modelType, elementClassMap, newMemberPath);
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
                    strBuilder.AppendLine($"  {dependency}");
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
                    strBuilder.AppendLine($"  {dependency}");
            }
            return strBuilder.ToString();
        }
    }
}
