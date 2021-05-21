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

using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    abstract class ModelMapsSchemaBase : SchemaBase, IModelMapsSchema
    {
        // Fields.
        private Dictionary<string, IModelMap> _allMapsDictionary = default!;
        protected readonly List<IModelMap> _secondaryMaps = new();

        // Constructor.
        protected ModelMapsSchemaBase(
            IModelMap activeMap,
            IDbContext dbContext,
            Type modelType)
            : base(modelType)
        {
            ActiveMap = activeMap ?? throw new ArgumentNullException(nameof(activeMap));
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Verify if have to use proxy model.
            if (modelType != typeof(object) &&
                !modelType.IsAbstract &&
                !dbContext.ProxyGenerator.IsProxyType(modelType))
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(modelType, dbContext).GetType();
                ActiveMap.UseProxyGenerator(dbContext);
            }
        }

        // Properties.
        public IBsonSerializer ActiveBsonClassMapSerializer => ActiveMap.BsonClassMapSerializer;
        public IModelMap ActiveMap { get; }
        public override IBsonSerializer? ActiveSerializer => ActiveMap.Serializer;
        public IReadOnlyDictionary<string, IModelMap> AllMapsDictionary
        {
            get
            {
                if (_allMapsDictionary is null)
                {
                    var result = SecondaryMaps
                        .Append(ActiveMap)
                        .ToDictionary(modelMap => modelMap.Id);

                    if (!IsFrozen)
                        return result;

                    //optimize performance only if frozen
                    _allMapsDictionary = result;
                }
                return _allMapsDictionary;
            }
        }
        public IDbContext DbContext { get; }
        public IBsonSerializer? FallbackSerializer { get; protected set; }
        public override Type? ProxyModelType { get; }
        public IEnumerable<IModelMap> SecondaryMaps => _secondaryMaps;

        // Protected methods.
        protected void AddFallbackCustomSerializerHelper(IBsonSerializer fallbackSerializer) =>
            ExecuteConfigAction(() =>
            {
                if (fallbackSerializer is null)
                    throw new ArgumentNullException(nameof(fallbackSerializer));
                if (FallbackSerializer != null)
                    throw new InvalidOperationException("Fallback serializer already setted");

                FallbackSerializer = fallbackSerializer;
            });

        protected void AddSecondaryMapHelper(IModelMap modelMap) =>
            ExecuteConfigAction(() =>
            {
                if (modelMap is null)
                    throw new ArgumentNullException(nameof(modelMap));

                // Verify if this schema uses proxy model.
                if (ProxyModelType != null)
                    modelMap.UseProxyGenerator(DbContext);

                // Add schema.
                _secondaryMaps.Add(modelMap);
                return this;
            });

        protected override void FreezeAction()
        {
            // Freeze model maps.
            ActiveMap.Freeze();
            foreach (var secondaryMap in _secondaryMaps)
                secondaryMap.Freeze();
        }
    }
}
