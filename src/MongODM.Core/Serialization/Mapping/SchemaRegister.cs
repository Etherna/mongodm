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


        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Don't need to dispose")]
        public IModelMapsSchema<TModel> AddModelMapSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null,
            string? baseModelMapId = null,
            IBsonSerializer<TModel>? customSerializer = null) where TModel : class =>
            AddModelMapSchema(new ModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId,
                customSerializer));

        public IModelMapsSchema<TModel> AddModelMapSchema<TModel>(
            ModelMap<TModel> activeModelMap)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelMap is null)
                    throw new ArgumentNullException(nameof(activeModelMap));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ModelMapsSchema<TModel>(activeModelMap, dbContext);
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        public ICustomSerializerSchema<TModel> GetCustomSerializerSchema<TModel>() where TModel : class =>
            (ICustomSerializerSchema<TModel>)Schemas[typeof(TModel)];

        public IEnumerable<MemberMap> GetMemberMapsFromMemberInfo(MemberInfo memberInfo)
        {
            Freeze();

            if (memberInfoToMemberMapsDictionary.TryGetValue(memberInfo, out List<MemberMap> dependencies))
                return dependencies;
            return Array.Empty<MemberMap>();
        }

        public IModelMapsSchema<TModel> GetModelMapsSchema<TModel>() where TModel : class =>
            (IModelMapsSchema<TModel>)Schemas[typeof(TModel)];

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
            // Link model maps with their base map.
            LinkBaseModelMaps();

            // Freeze, register serializers and compile registers.
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
                if (memberSerializer is IModelMapsContainerSerializer schemaSerializer)
                {
                    var useCascadeDelete = (memberSerializer as IReferenceContainerSerializer)?.UseCascadeDelete;
                    foreach (var childClassMap in schemaSerializer.AllChildClassMaps)
                        CompileDependencyRegisters(modelMap, childClassMap, lastEntityClassMap, currentMemberPath, useCascadeDelete);
                }
            }
        }

        private IModelMapsSchema CreateNewDefaultModelMapSchema(Type modelType)
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
                    baseSchema = CreateNewDefaultModelMapSchema(baseModelType);

                    // Register schema instance.
                    _schemas.Add(baseModelType, baseSchema);
                    processingSchemas.Push((IModelMapsSchema)baseSchema);
                }

                // Process schema's model maps.
                foreach (var modelMap in schema.AllMapsDictionary.Values)
                {
                    // Search base model map.
                    ModelMap baseModelMap = modelMap.BaseModelMapId != null ?
                        ((IModelMapsSchema)baseSchema).AllMapsDictionary[modelMap.BaseModelMapId] :
                        ((IModelMapsSchema)baseSchema).ActiveMap;

                    // Link base model map.
                    modelMap.SetBaseModelMap(baseModelMap);
                }
            }
        }
    }
}
