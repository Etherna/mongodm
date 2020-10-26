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

using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Serialization
{
    public class ModelSchemaConfigurationRegister : IModelSchemaConfigurationRegister, IDisposable
    {
        // Fields.
        private readonly ReaderWriterLockSlim configLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly Dictionary<MemberInfo, List<ModelSchemaMemberMap>> memberDependenciesMap =
            new Dictionary<MemberInfo, List<ModelSchemaMemberMap>>();
        private readonly Dictionary<Type, List<ModelSchemaMemberMap>> modelDependenciesMap =
            new Dictionary<Type, List<ModelSchemaMemberMap>>();
        private readonly Dictionary<Type, List<ModelSchemaMemberMap>> modelEntityReferencesIdsMap =
            new Dictionary<Type, List<ModelSchemaMemberMap>>();

        private IDbContext dbContext = default!;
        private readonly IModelSchemaBuilder modelSchemaBuilder;
        private readonly ISerializerModifierAccessor serializerModifierAccessor;
        private readonly List<ModelSchema> schemas = new List<ModelSchema>();

        // Constructors and initialization.
        public ModelSchemaConfigurationRegister(
            IModelSchemaBuilder modelSchemaBuilder,
            ISerializerModifierAccessor serializerModifierAccessor)
        {
            this.modelSchemaBuilder = modelSchemaBuilder;
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

        // Methods.
        public void Dispose()
        {
            configLock.Dispose();
        }

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
                        // Register regular model.
                        //register class map
                        BsonClassMap.RegisterClassMap(schemaGroup.ClassMap);

                        //register serializer
                        if (schemaGroup.Serializer != null)
                            BsonSerializer.RegisterSerializer(schemaGroup.ModelType, schemaGroup.Serializer);

                        // Register proxy model.
                        //register proxy class map
                        if (schemaGroup.ProxyClassMap != null)
                            BsonClassMap.RegisterClassMap(schemaGroup.ProxyClassMap);
                    }

                    // Compile dependency registers.
                    foreach (var schema in schemas)
                    {
                        schema.ClassMap.Freeze();

                        CompileDependencyRegisters(
                            schema.ModelType,
                            Array.Empty<EntityMember>(),
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

        public IModelSchemaConfiguration<TModel> AddModel<TModel>(
            string id,
            Action<BsonClassMap<TModel>>? classMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null)
            where TModel : class =>
            AddModel(modelSchemaBuilder.GenerateModelSchema(id, classMapInitializer, customSerializer));

        public IModelSchemaConfiguration<TModel> AddModel<TModel>(ModelSchema<TModel> modelSchema)
            where TModel : class
        {
            throw new NotImplementedException();
        }
        public void RegisterModelSchema<TModel>(
            SemanticVersion fromVersion,
            BsonClassMap<TModel> classMap,
            Func<IBsonSerializer<TModel>>? initCustomSerializer = null,
            Func<TModel, SemanticVersion?, Task<TModel>>? modelMigrationAsync = null)
            where TModel : class
        {
            if (classMap is null)
                throw new ArgumentNullException(nameof(classMap));

            configLock.EnterWriteLock();
            try
            {
                if (IsFrozen)
                    throw new InvalidOperationException("Register is frozen");

                // If not abstract, adjustments for use proxygenerator.
                BsonClassMap? proxyClassMap = null;
                if (!typeof(TModel).IsAbstract)
                {
                    //set creator
                    classMap.SetCreator(() => dbContext.ProxyGenerator.CreateInstance<TModel>(dbContext));

                    //generate proxy classmap
                    proxyClassMap = new BsonClassMap(
                        dbContext.ProxyGenerator.CreateInstance<TModel>(dbContext).GetType());
                }

                // Generate model serializer.
                IBsonSerializer<TModel>? serializer = null;

                if (initCustomSerializer != null) //if custom is setted, keep it
                    serializer = initCustomSerializer();

                else if (!typeof(TModel).IsAbstract) //else if can deserialize, set default serializer
                    serializer =
                        new ExtendedClassMapSerializer<TModel>(
                            dbContext.DbCache,
                            dbContext.ApplicationVersion,
                            serializerModifierAccessor,
                            (m, v) => modelMigrationAsync?.Invoke(m, v) ?? Task.FromResult(m))
                        { AddVersion = typeof(IEntityModel).IsAssignableFrom(typeof(TModel)) }; //true only for entity models

                // Register schema.
                schemas.Add(new ModelSchema(classMap, typeof(TModel), proxyClassMap, serializer, fromVersion));
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
            SemanticVersion version,
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
