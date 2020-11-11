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
        protected readonly List<IModelMap> _secondaryMaps = new List<IModelMap>();

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
            if (!modelType.IsAbstract &&
                !dbContext.ProxyGenerator.IsProxyType(modelType))
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(modelType, dbContext).GetType();
                ActiveMap.UseProxyGenerator(dbContext);
            }
        }

        // Properties.
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
