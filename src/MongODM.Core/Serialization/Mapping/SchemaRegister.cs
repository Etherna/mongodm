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
        private readonly Dictionary<Type, ISchema> _schemas = new Dictionary<Type, ISchema>();

        private readonly Dictionary<MemberInfo, List<MemberMap>> memberInfoToMemberMapsDictionary =
            new Dictionary<MemberInfo, List<MemberMap>>();
        private readonly Dictionary<Type, List<MemberMap>> modelTypeToReferencedIdMemberMapsDictionary =
            new Dictionary<Type, List<MemberMap>>();

        private IDbContext dbContext = default!;

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

        public IReadOnlyDictionary<Type, ISchema> Schemas
        {
            get
            {
                Freeze();
                return _schemas;
            }
        }

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
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

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
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        public IEnumerable<MemberMap> GetMemberMapsFromMemberInfo(MemberInfo memberInfo)
        {
            Freeze();

            if (memberInfoToMemberMapsDictionary.TryGetValue(memberInfo, out List<MemberMap> dependencies))
                return dependencies;
            return Array.Empty<MemberMap>();
        }

        public IEnumerable<MemberMap> GetReferencedIdMemberMapsFromRootModel(Type modelType)
        {
            Freeze();

            if (modelTypeToReferencedIdMemberMapsDictionary.TryGetValue(modelType, out List<MemberMap> dependencies))
                return dependencies;
            return Array.Empty<MemberMap>();
        }

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();

            // Member dependencies.
            strBuilder.AppendLine("Member dependencies:");
            foreach (var dependencies in from dependency in memberInfoToMemberMapsDictionary
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
            foreach (var schema in _schemas.Values)
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
                //memberInfo to related member maps, for each different model maps version
                if (!memberInfoToMemberMapsDictionary.ContainsKey(bsonMemberMap.MemberInfo))
                    memberInfoToMemberMapsDictionary[bsonMemberMap.MemberInfo] = new List<MemberMap>();

                memberInfoToMemberMapsDictionary[bsonMemberMap.MemberInfo].Add(memberMap);

                //model type to each referenced id member maps, for each different model maps version
                if (!modelTypeToReferencedIdMemberMapsDictionary.ContainsKey(modelMap.ModelType))
                    modelTypeToReferencedIdMemberMapsDictionary[modelMap.ModelType] = new List<MemberMap>();

                if (memberMap.IsEntityReferenceMember && memberMap.IsIdMember)
                    modelTypeToReferencedIdMemberMapsDictionary[modelMap.ModelType].Add(memberMap);

                // Analize recursion on member.
                var memberSerializer = bsonMemberMap.GetSerializer();

                //model maps schema serializers
                if (memberSerializer is IModelMapsSchemaSerializer schemaSerializer &&
                    schemaSerializer.ModelMapsSchema != null)
                {
                    var useCascadeDelete = (memberSerializer as IReferenceContainerSerializer)?.UseCascadeDelete;
                    foreach (var childModelMap in schemaSerializer.ModelMapsSchema.AllMapsDictionary.Values)
                        CompileDependencyRegisters(modelMap, childModelMap.BsonClassMap, lastEntityClassMap, currentMemberPath, useCascadeDelete);
                }
            }
        }
    }
}
