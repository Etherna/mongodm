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

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    abstract class ModelMapsSchemaBase : SchemaBase, IModelMapsSchema
    {
        // Fields.
        private readonly Dictionary<string, IMemberMap> _allMemberMapsDictionary = new(); // PathId -> MemberMap
        private Dictionary<string, IModelMap> _allModelMapsDictionary = default!; // Id -> ModelMap
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
            }

            activeMap.TryUseProxyGenerator(dbContext);
        }

        // Properties.
        public IBsonSerializer ActiveBsonClassMapSerializer => ActiveMap.BsonClassMapSerializer;
        public IModelMap ActiveMap { get; }
        public override IBsonSerializer? ActiveSerializer => ActiveMap.Serializer;
        public IEnumerable<IMemberMap> AllMemberMaps => _allMemberMapsDictionary.Values;
        public IReadOnlyDictionary<string, IModelMap> AllModelMapsDictionary
        {
            get
            {
                if (_allModelMapsDictionary is null)
                {
                    var result = SecondaryMaps
                        .Append(ActiveMap)
                        .ToDictionary(modelMap => modelMap.Id);

                    if (!IsFrozen)
                        return result;

                    //optimize performance only if frozen
                    _allModelMapsDictionary = result;
                }
                return _allModelMapsDictionary;
            }
        }
        public IDbContext DbContext { get; }
        public IModelMap? FallbackModelMap { get; protected set; }
        public IBsonSerializer? FallbackSerializer { get; protected set; }
        public override Type? ProxyModelType { get; }
        public IEnumerable<IMemberMap> ReferencedIdMemberMaps => _allMemberMapsDictionary.Values.Where(memberMap => memberMap.IsEntityReferenceMember && memberMap.IsIdMember);
        public IEnumerable<IModelMap> SecondaryMaps => _secondaryMaps;

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
                _secondaryMaps.Add(modelMap);
                return this;
            });

        protected override void FreezeAction()
        {
            // Freeze model maps.
            ActiveMap.Freeze();

            foreach (var secondaryMap in _secondaryMaps)
                secondaryMap.Freeze();

            FallbackModelMap?.Freeze();

            // Initialize member maps and registers.
            foreach (var modelMap in AllModelMapsDictionary.Values)
                InitializeMemberMaps(
                    modelMap,
                    Array.Empty<(IModelMap OwnerClass, BsonMemberMap Member)>());
        }

        // Helpers.
        /// <summary>
        /// Explore with recursion a model map and all its descendent definitions through member maps
        /// </summary>
        /// <param name="modelMap"></param>
        /// <param name="memberPath"></param>
        /// <param name="useCascadeDeleteSetting"></param>
        private void InitializeMemberMaps(
            IModelMap modelMap,
            IEnumerable<(IModelMap OwnerClass, BsonMemberMap Member)> memberPath,
            bool? useCascadeDeleteSetting = null)
        {
            // Ignore model maps of abstract types. (child classes will map all their members)
            if (modelMap.ModelType.IsAbstract)
                return;
            // Ignore model maps of proxy types.
            if (DbContext.ProxyGenerator.IsProxyType(modelMap.ModelType))
                return;

            // Explore recursively members.
            foreach (var bsonMemberMap in modelMap.BsonClassMap.AllMemberMaps)
            {
                // Update path.
                var currentMemberPath = memberPath.Append((modelMap, bsonMemberMap));

                // Identify current member with its path from current model map, and cascade delete information.
                var memberMap = new MemberMap(
                    new MemberPath(currentMemberPath),
                    useCascadeDeleteSetting ?? false);

                // Add member map to dictionary.
                _allMemberMapsDictionary.Add(memberMap.DefinitionPath.TypedPathAsString, memberMap);

                // Analize recursion on member.
                var memberSerializer = bsonMemberMap.GetSerializer();
                if (memberSerializer is IModelMapsContainerSerializer modelMapsContainerSerializer)
                    foreach (var childModelMap in modelMapsContainerSerializer.AllChildModelMaps)
                        InitializeMemberMaps(
                            childModelMap,
                            currentMemberPath,
                            (memberSerializer as IReferenceContainerSerializer)?.UseCascadeDelete);
            }
        }
    }
}
