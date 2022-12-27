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

using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    internal abstract class ModelMap : MapBase, IModelMap
    {
        // Fields.
        private IModelMapSchema _activeSchema = default!;
        private readonly List<IMemberMap> _definedMemberMaps = new();
        private Dictionary<string, IModelMapSchema> _schemasById = default!;
        protected readonly List<IModelMapSchema> _secondarySchemas = new();

        // Constructor.
        protected ModelMap(
            IDbContext dbContext,
            Type modelType)
            : base(modelType)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Verify if have to use proxy model.
            if (modelType != typeof(object) &&
                !modelType.IsAbstract &&
                !dbContext.ProxyGenerator.IsProxyType(modelType))
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(modelType, dbContext).GetType();
            }
        }

        // Properties.
        public IModelMapSchema ActiveSchema
        {
            get => _activeSchema;
            internal set
            {
                _activeSchema = value;
                _activeSchema.TryUseProxyGenerator(DbContext);
            }
        }
        public override IBsonSerializer ActiveSerializer => ActiveSchema.Serializer;
        public IEnumerable<IMemberMap> AllDescendingMemberMaps => DefinedMemberMaps.SelectMany(mm => mm.AllDescendingMemberMaps);
        public IDbContext DbContext { get; }
        public IEnumerable<IMemberMap> DefinedMemberMaps
        {
            get
            {
                Freeze(); //needed for initialization
                return _definedMemberMaps!;
            }
        }
        public IModelMapSchema? FallbackSchema { get; protected set; }
        public IBsonSerializer? FallbackSerializer { get; protected set; }
        public override Type? ProxyModelType { get; }
        public IReadOnlyDictionary<string, IModelMapSchema> SchemasById
        {
            get
            {
                if (_schemasById is null)
                {
                    var modelMaps = new[] { ActiveSchema }.Concat(_secondarySchemas);

                    if (FallbackSchema is not null)
                        modelMaps = modelMaps.Append(FallbackSchema);

                    var result = modelMaps.ToDictionary(modelMap => modelMap.Id);

                    if (!IsFrozen)
                        return result;

                    //optimize performance only if frozen
                    _schemasById = result;
                }
                return _schemasById;
            }
        }
        public IEnumerable<IModelMapSchema> SecondarySchemas => _secondarySchemas;

        // Protected methods.
        protected void AddFallbackCustomSerializerHelper(IBsonSerializer fallbackSerializer) =>
            ExecuteConfigAction(() =>
            {
                if (fallbackSerializer is null)
                    throw new ArgumentNullException(nameof(fallbackSerializer));
                if (FallbackSerializer is not null)
                    throw new InvalidOperationException("Fallback serializer already setted");

                FallbackSerializer = fallbackSerializer;
            });

        protected void AddFallbackModelMapSchemaHelper(IModelMapSchema fallbackSchema) =>
            ExecuteConfigAction(() =>
            {
                if (fallbackSchema is null)
                    throw new ArgumentNullException(nameof(fallbackSchema));
                if (FallbackSchema is not null)
                    throw new InvalidOperationException("Fallback model map schema already setted");

                FallbackSchema = fallbackSchema;
            });

        protected void AddSecondarySchemaHelper(IModelMapSchema schema) =>
            ExecuteConfigAction(() =>
            {
                if (schema is null)
                    throw new ArgumentNullException(nameof(schema));

                // Try to use proxy model generator.
                schema.TryUseProxyGenerator(DbContext);

                // Add schema.
                _secondarySchemas.Add(schema);
                return this;
            });

        protected override void FreezeAction()
        {
            // Initialize defined member maps.
            foreach (var schema in SchemasById.Values)
            {
                schema.Freeze();
                foreach (var bsonMemberMap in schema.BsonClassMap.AllMemberMaps)
                {
                    var memberMap = BuildMemberMap(bsonMemberMap, schema, null);
                    _definedMemberMaps.Add(memberMap);
                    ((ModelMapSchema)schema).AddGeneratedMemberMap(memberMap);
                }
            }
        }

        // Helpers.
        private static IMemberMap BuildMemberMap(
            BsonMemberMap bsonMemberMap,
            IModelMapSchema modelMapSchema,
            IMemberMap? parentMemberMap)
        {
            var memberMap = new MemberMap(bsonMemberMap, modelMapSchema, parentMemberMap);

            // Analize recursion on member.
            var memberSerializer = bsonMemberMap.GetSerializer();
            if (memberSerializer is IModelMapsContainerSerializer modelMapsContainerSerializer)
                foreach (var schema in modelMapsContainerSerializer.AllChildModelMapSchemas)
                {
                    schema.Freeze();
                    foreach (var childBsonMemberMap in schema.BsonClassMap.AllMemberMaps)
                    {
                        var childMemberMap = BuildMemberMap(childBsonMemberMap, schema, memberMap);
                        memberMap.AddChildMemberMap(childMemberMap);
                        ((ModelMapSchema)schema).AddGeneratedMemberMap(memberMap);
                    }
                }

            return memberMap;
        }
    }

    internal class ModelMap<TModel> : ModelMap, IModelMapBuilder<TModel>
        where TModel : class
    {
        // Constructor.
        public ModelMap(IDbContext dbContext)
            : base(dbContext, typeof(TModel))
        { }

        // Methods.
        public IModelMapBuilder<TModel> AddFallbackCustomSerializerMap(IBsonSerializer<TModel> fallbackSerializer)
        {
            AddFallbackCustomSerializerHelper(fallbackSerializer);
            return this;
        }

        public IModelMapBuilder<TModel> AddFallbackModelMapSchema(
            Action<BsonClassMap<TModel>>? modelMapSchemaInitializer = null,
            string? baseModelMapSchemaId = null)
        {
            AddFallbackModelMapSchemaHelper(new ModelMapSchema<TModel>(
                "fallback",
                new BsonClassMap<TModel>(modelMapSchemaInitializer ?? (cm => cm.AutoMap())),
                baseModelMapSchemaId,
                null,
                null,
                this));
            return this;
        }

        public IModelMapBuilder<TModel> AddSecondaryModelMapSchema(
            string id,
            Action<BsonClassMap<TModel>>? modelMapSchemaInitializer = null,
            string? baseModelMapSchemaId = null)
        {
            AddFallbackModelMapSchemaHelper(new ModelMapSchema<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapSchemaInitializer ?? (cm => cm.AutoMap())),
                baseModelMapSchemaId,
                null,
                null,
                this));
            return this;
        }
    }
}
