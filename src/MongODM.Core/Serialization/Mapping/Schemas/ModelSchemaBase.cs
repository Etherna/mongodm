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

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    abstract class ModelSchemaBase : SchemaBase, IModelSchema
    {
        // Fields.
        private Dictionary<string, IModelMap> _rootModelMapsDictionary = default!; // Id -> ModelMap
        protected readonly List<IModelMap> _secondaryModelMaps = new();

        // Constructor.
        protected ModelSchemaBase(
            IModelMap activeMap,
            IDbContext dbContext,
            Type modelType)
            : base(modelType)
        {
            ActiveModelMap = activeMap ?? throw new ArgumentNullException(nameof(activeMap));
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Verify if have to use proxy model.
            if (modelType != typeof(object) &&
                !modelType.IsAbstract &&
                !dbContext.ProxyGenerator.IsProxyType(modelType))
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(modelType, dbContext).GetType();
            }

            activeMap.TryUseProxyGenerator(dbContext);
        }

        // Properties.
        public IModelMap ActiveModelMap { get; }
        public override IBsonSerializer? ActiveSerializer => ActiveModelMap.Serializer;
        public IDbContext DbContext { get; }
        public IModelMap? FallbackModelMap { get; protected set; }
        public IBsonSerializer? FallbackSerializer { get; protected set; }
        public override Type? ProxyModelType { get; }
        public IReadOnlyDictionary<string, IModelMap> RootModelMapsDictionary
        {
            get
            {
                if (_rootModelMapsDictionary is null)
                {
                    var modelMaps = new[] { ActiveModelMap }.Concat(_secondaryModelMaps);

                    if (FallbackModelMap is not null)
                        modelMaps = modelMaps.Append(FallbackModelMap);

                    var result = modelMaps.ToDictionary(modelMap => modelMap.Id);

                    if (!IsFrozen)
                        return result;

                    //optimize performance only if frozen
                    _rootModelMapsDictionary = result;
                }
                return _rootModelMapsDictionary;
            }
        }
        public IEnumerable<IModelMap> SecondaryModelMaps => _secondaryModelMaps;

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

        protected void AddFallbackModelMapHelper(IModelMap fallbackModelMap) =>
            ExecuteConfigAction(() =>
            {
                if (fallbackModelMap is null)
                    throw new ArgumentNullException(nameof(fallbackModelMap));
                if (FallbackModelMap is not null)
                    throw new InvalidOperationException("Fallback model map already setted");

                FallbackModelMap = fallbackModelMap;
            });

        protected void AddSecondaryMapHelper(IModelMap modelMap) =>
            ExecuteConfigAction(() =>
            {
                if (modelMap is null)
                    throw new ArgumentNullException(nameof(modelMap));

                // Try to use proxy model generator.
                modelMap.TryUseProxyGenerator(DbContext);

                // Add schema.
                _secondaryModelMaps.Add(modelMap);
                return this;
            });

        protected override void FreezeAction()
        {
            // Freeze model maps.
            foreach (var modelMap in RootModelMapsDictionary.Values)
                modelMap.Freeze();

            // Initialize member maps.
            foreach (var modelMap in RootModelMapsDictionary.Values)
            {
                // Ignore model maps of abstract types. (child classes will map all their members)
                if (modelMap.ModelType.IsAbstract)
                    return;
                // Ignore model maps of proxy types.
                if (DbContext.ProxyGenerator.IsProxyType(modelMap.ModelType))
                    return;

                ((ModelMap)modelMap).InitializeMemberMaps(new MemberPath(Array.Empty<(IModelMap OwnerModel, BsonMemberMap Member)>()));
            }
        }
    }
}
