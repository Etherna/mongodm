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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    internal abstract class ModelMap : MapBase, IModelMap
    {
        // Fields.
        private IModelMapSchema _activeModelMapSchema = default!;
        private Dictionary<string, IModelMapSchema> _allModelMapSchemaDictionary = default!; // Id -> ModelMap
        protected readonly List<IModelMapSchema> _secondaryModelMapSchemas = new();

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
        public IModelMapSchema ActiveModelMapSchema
        {
            get => _activeModelMapSchema;
            internal set
            {
                _activeModelMapSchema = value;
                _activeModelMapSchema.TryUseProxyGenerator(DbContext);
            }
        }
        public override IBsonSerializer ActiveSerializer => ActiveModelMapSchema.Serializer;
        public IDbContext DbContext { get; }
        public IModelMapSchema? FallbackModelMapSchema { get; protected set; }
        public IBsonSerializer? FallbackSerializer { get; protected set; }
        public override Type? ProxyModelType { get; }
        public IReadOnlyDictionary<string, IModelMapSchema> AllModelMapSchemaDictionary
        {
            get
            {
                if (_allModelMapSchemaDictionary is null)
                {
                    var modelMaps = new[] { ActiveModelMapSchema }.Concat(_secondaryModelMapSchemas);

                    if (FallbackModelMapSchema is not null)
                        modelMaps = modelMaps.Append(FallbackModelMapSchema);

                    var result = modelMaps.ToDictionary(modelMap => modelMap.Id);

                    if (!IsFrozen)
                        return result;

                    //optimize performance only if frozen
                    _allModelMapSchemaDictionary = result;
                }
                return _allModelMapSchemaDictionary;
            }
        }
        public IEnumerable<IModelMapSchema> SecondaryModelMapSchemas => _secondaryModelMapSchemas;

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

        protected void AddFallbackModelMapSchemaHelper(IModelMapSchema fallbackModelMapSchema) =>
            ExecuteConfigAction(() =>
            {
                if (fallbackModelMapSchema is null)
                    throw new ArgumentNullException(nameof(fallbackModelMapSchema));
                if (FallbackModelMapSchema is not null)
                    throw new InvalidOperationException("Fallback model map schema already setted");

                FallbackModelMapSchema = fallbackModelMapSchema;
            });

        protected void AddSecondaryModelMapSchemaHelper(IModelMapSchema modelMapSchema) =>
            ExecuteConfigAction(() =>
            {
                if (modelMapSchema is null)
                    throw new ArgumentNullException(nameof(modelMapSchema));

                // Try to use proxy model generator.
                modelMapSchema.TryUseProxyGenerator(DbContext);

                // Add schema.
                _secondaryModelMapSchemas.Add(modelMapSchema);
                return this;
            });

        protected override void FreezeAction()
        {
            // Freeze model maps.
            foreach (var modelMap in AllModelMapSchemaDictionary.Values)
                modelMap.Freeze();

            // Initialize member maps.
            foreach (var modelMap in AllModelMapSchemaDictionary.Values)
            {
                // Ignore model maps of abstract types. (child classes will map all their members)
                if (modelMap.ModelType.IsAbstract)
                    return;
                // Ignore model maps of proxy types.
                if (DbContext.ProxyGenerator.IsProxyType(modelMap.ModelType))
                    return;

                ((ModelMapSchema)modelMap).InitializeMemberMaps(new MemberPath(Array.Empty<(IModelMapSchema OwnerModel, BsonMemberMap Member)>()));
            }
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
