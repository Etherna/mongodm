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

using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Serialization.Mapping.Schemas;
using Etherna.MongODM.Core.Serialization.Serializers;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class SchemaRegister : FreezableConfig, ISchemaRegister
    {
        // Fields.
        private readonly Dictionary<MemberInfo, List<MemberMap>> memberDependenciesMap =
            new Dictionary<MemberInfo, List<MemberMap>>();
        private readonly Dictionary<Type, List<MemberMap>> modelDependenciesMap =
            new Dictionary<Type, List<MemberMap>>();
        private readonly Dictionary<Type, List<MemberMap>> modelEntityReferencesIdsMap =
            new Dictionary<Type, List<MemberMap>>();

        private IDbContext dbContext = default!;
        private readonly Dictionary<Type, ISchema> schemasByModelType = new Dictionary<Type, ISchema>();

        // Constructor, initialization.
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
        public ICustomSerializerSchema<TModel> AddCustomSerializerSchema<TModel>(
            IBsonSerializer<TModel> customSerializer,
            bool requireCollectionMigration = false) where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (customSerializer is null)
                    throw new ArgumentNullException(nameof(customSerializer));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new CustomSerializerSchema<TModel>(customSerializer, requireCollectionMigration);
                schemasByModelType.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });


        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Don't need to dispose")]
        public IModelMapsSchema<TModel> AddModelMapSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null,
            bool requireCollectionMigration = false) where TModel : class =>
            AddModelMapSchema(new ModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap())),
                customSerializer), requireCollectionMigration);

        public IModelMapsSchema<TModel> AddModelMapSchema<TModel>(
            ModelMap<TModel> activeModelMap,
            bool requireCollectionMigration = false) where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelMap is null)
                    throw new ArgumentNullException(nameof(activeModelMap));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ModelMapsSchema<TModel>(
                    activeModelMap, dbContext, requireCollectionMigration);
                schemasByModelType.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        public IEnumerable<MemberMap> GetMemberDependencies(MemberInfo memberInfo)
        {
            Freeze();

            if (memberDependenciesMap.TryGetValue(memberInfo, out List<MemberMap> dependencies))
                return dependencies;
            return Array.Empty<MemberMap>();
        }

        public IEnumerable<MemberMap> GetModelDependencies(Type modelType)
        {
            Freeze();

            if (modelDependenciesMap.TryGetValue(modelType, out List<MemberMap> dependencies))
                return dependencies;
            return Array.Empty<MemberMap>();
        }

        public IEnumerable<MemberMap> GetModelEntityReferencesIds(Type modelType)
        {
            Freeze();

            if (modelEntityReferencesIdsMap.TryGetValue(modelType, out List<MemberMap> dependencies))
                return dependencies;
            return Array.Empty<MemberMap>();
        }

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();

            // Model dependencies.
            strBuilder.AppendLine("Model dependencies:");
            foreach (var dependencies in from dependency in modelDependenciesMap
                                         orderby $"{dependency.Key.Name}"
                                         select dependency)
            {
                strBuilder.AppendLine($"{dependencies.Key.Name}");
                foreach (var dependency in dependencies.Value)
                    strBuilder.AppendLine($"  {dependency}");
            }
            strBuilder.AppendLine();

            // Member dependencies.
            strBuilder.AppendLine("Member dependencies:");
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

        // Protected methods.
        protected override void FreezeAction()
        {
            foreach (var schema in schemasByModelType.Values)
            {
                // Freeze schema.
                schema.Freeze();

                // Register active serializers.
                if (schema.ActiveSerializer != null)
                {
                    //regular model
                    BsonSerializer.RegisterSerializer(schema.ModelType, schema.ActiveSerializer);

                    //proxy model
                    if (schema.ProxyModelType != null)
                        BsonSerializer.RegisterSerializer(schema.ProxyModelType, schema.ActiveSerializer);
                }

                // Compile dependency registers.
                /* Only model map based schemas can be analyzed for document dependencies.
                 * Schemas based on custom serializers can't be explored.
                 */
                if (schema is IModelMapsSchema modelMapSchema)
                    foreach (var modelMap in modelMapSchema.AllMapsDictionary.Values)
                        CompileDependencyRegisters(
                            modelMap,
                            modelMap.BsonClassMap,
                            default,
                            Array.Empty<BsonMemberMap>());
            }
        }

        // Helpers.
        private void CompileDependencyRegisters(
            ModelMap modelMap,
            BsonClassMap currentClassMap,
            BsonClassMap? lastEntityClassMap,
            IEnumerable<BsonMemberMap> bsonMemberPath,
            bool? useCascadeDeleteSetting = null)
        {
            // Ignore class maps of abstract types. (child classes will map all their members)
            if (currentClassMap.ClassType.IsAbstract)
                return;

            // Identify last indented entity class maps.
            if (currentClassMap.IsEntity())
                lastEntityClassMap = currentClassMap;

            // Explore recursively members.
            foreach (var bsonMemberMap in currentClassMap.AllMemberMaps)
            {
                // Identify if exists a cyclicity with member path.
                if (bsonMemberPath.Contains(bsonMemberMap))
                {
                    var memberPathString = string.Join("->",
                        bsonMemberPath.Append(bsonMemberMap)
                            .Select(m => $"[{m.ClassMap.ClassType.Name}]{m.MemberName}"));

                    throw new InvalidOperationException("Invalid cyclicity identified with model map definition:\n" + memberPathString);
                }

                // Update path.
                var currentMemberPath = bsonMemberPath.Append(bsonMemberMap);

                // Identify current member with its root model map, the path from current model map, and cascade delete information.
                var memberMap = new MemberMap(
                    modelMap,
                    currentMemberPath,
                    useCascadeDeleteSetting);

                // Add member dependency to registers.
                //1
                if (!memberDependenciesMap.ContainsKey(bsonMemberMap.MemberInfo))
                    memberDependenciesMap[bsonMemberMap.MemberInfo] = new List<MemberMap>();

                memberDependenciesMap[bsonMemberMap.MemberInfo].Add(memberMap);

                //2
                if (!modelDependenciesMap.ContainsKey(modelMap.ModelType))
                    modelDependenciesMap[modelMap.ModelType] = new List<MemberMap>();

                modelDependenciesMap[modelMap.ModelType].Add(memberMap);

                //3
                if (!modelEntityReferencesIdsMap.ContainsKey(modelMap.ModelType))
                    modelEntityReferencesIdsMap[modelMap.ModelType] = new List<MemberMap>();

                if (memberMap.IsEntityReferenceMember && memberMap.IsIdMember)
                    modelEntityReferencesIdsMap[modelMap.ModelType].Add(memberMap);

                // Analize recursion on member.
                var memberSerializer = bsonMemberMap.GetSerializer();
                var serializerType = memberSerializer.GetType();
                
                //custom serializers
                if (memberSerializer is IClassMapContainerSerializer classMapContainer)
                {
                    var useCascadeDelete = (memberSerializer as IReferenceContainerSerializer)?.UseCascadeDelete;
                    foreach (var childClassMap in classMapContainer.ContainedClassMaps)
                        CompileDependencyRegisters(modelMap, childClassMap, lastEntityClassMap, currentMemberPath, useCascadeDelete);
                }

                //default serializers
                else if (serializerType.IsGenericType &&
                    serializerType.GetGenericTypeDefinition() == typeof(BsonClassMapSerializer<>)) //default classmapp
                {
                    var memberClassMap = BsonClassMap.LookupClassMap(bsonMemberMap.MemberType);
                    CompileDependencyRegisters(modelMap, memberClassMap, lastEntityClassMap, currentMemberPath);
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
                        CompileDependencyRegisters(modelMap, elementClassMap, lastEntityClassMap, currentMemberPath);
                    }
                }
            }
        }
    }
}
