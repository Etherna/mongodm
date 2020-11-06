using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Serializers.Config
{
    public class ReferenceSchema<TModel> : FreezableConfig, IReferenceSchema<TModel>
    {
        // Fields.
        private readonly ReferenceModelMap<TModel> _activeMap;
        private IDictionary<string, ReferenceModelMap> _allMapsDictionary = default!;
        private IBsonSerializer<TModel>? _fallbackSerializer;
        private readonly List<ReferenceModelMap> _secondaryMaps = new List<ReferenceModelMap>();
        private readonly IDbContext dbContext;

        // Constructor.
        public ReferenceSchema(
            ReferenceModelMap<TModel> activeMap,
            IDbContext dbContext)
        {
            _activeMap = activeMap ?? throw new ArgumentNullException(nameof(activeMap));
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Verify if have to use proxy model.
            if (UseProxyModel)
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext).GetType();
                activeMap.UseProxyGenerator(dbContext);
            }
        }

        // Properties.
        public ReferenceModelMap ActiveMap
        {
            get
            {
                Freeze();
                return _activeMap;
            }
        }

        public IDictionary<string, ReferenceModelMap> AllMapsDictionary
        {
            get
            {
                Freeze();

                if (_allMapsDictionary is null)
                {
                    // Build schema dictionary.
                    _allMapsDictionary = SecondaryMaps
                        .Append(ActiveMap)
                        .ToDictionary(modelMap => modelMap.Id);
                }
                return _allMapsDictionary;
            }
        }

        public IBsonSerializer? FallbackSerializer
        {
            get
            {
                Freeze();
                return _fallbackSerializer;
            }
        }

        public Type ModelType => typeof(TModel);

        public Type? ProxyModelType { get; }

        public IEnumerable<ReferenceModelMap> SecondaryMaps
        {
            get
            {
                Freeze();
                return _secondaryMaps;
            }
        }

        public bool UseProxyModel => !typeof(TModel).IsAbstract;

        // Methods.
        public IReferenceSchema<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer) =>
            ExecuteConfigAction(() =>
            {
                if (fallbackSerializer is null)
                    throw new ArgumentNullException(nameof(fallbackSerializer));
                if (_fallbackSerializer != null)
                    throw new InvalidOperationException("Fallback serializer already setted");

                _fallbackSerializer = fallbackSerializer;

                return this;
            });

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Don't dispose here")]
        public IReferenceSchema<TModel> AddSecondaryMap(
            string id,
            string? baseModelMapId = null,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null) =>
            AddSecondaryMap(new ReferenceModelMap<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId));

        public IReferenceSchema<TModel> AddSecondaryMap(ReferenceModelMap<TModel> modelMap) =>
            ExecuteConfigAction(() =>
            {
                if (modelMap is null)
                    throw new ArgumentNullException(nameof(modelMap));

                // Verify if have to use proxy model.
                if (UseProxyModel)
                    modelMap.UseProxyGenerator(dbContext);

                // Add schema.
                _secondaryMaps.Add(modelMap);
                return this;
            });

        // Protected methods.
        protected override void FreezeAction()
        {
            _activeMap.Freeze();
            foreach (var secondaryMap in _secondaryMaps)
                secondaryMap.Freeze();
        }
    }
}
