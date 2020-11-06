using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    class ModelMapsSchema<TModel> : SchemaBase, IModelMapsSchema<TModel>
        where TModel : class
    {
        // Fields.
        private readonly ModelMap<TModel> _activeMap;
        private IDictionary<string, ModelMap> _allMapsDictionary = default!;
        private IBsonSerializer<TModel>? _fallbackSerializer;
        private readonly List<ModelMap> _secondaryMaps = new List<ModelMap>();
        private readonly IDbContext dbContext;

        // Constructor.
        public ModelMapsSchema(
            ModelMap<TModel> activeMap,
            IDbContext dbContext)
            : base(typeof(TModel))
        {
            _activeMap = activeMap ?? throw new ArgumentNullException(nameof(activeMap));
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Verify if have to use proxy model.
            if (UseProxyModel)
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext).GetType();
                activeMap.UseProxyGenerator(dbContext);
            }

            // Verify if needs to use default serializer.
            if (!typeof(TModel).IsAbstract && activeMap.Serializer is null)
                activeMap.UseDefaultSerializer(dbContext);
        }

        // Properties.
        public ModelMap ActiveMap
        {
            get
            {
                Freeze();
                return _activeMap;
            }
        }
        public override IBsonSerializer? ActiveSerializer => ActiveMap.Serializer;
        public IDictionary<string, ModelMap> AllMapsDictionary
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

        public IBsonSerializer<TModel>? FallbackSerializer
        {
            get
            {
                Freeze();
                return _fallbackSerializer;
            }
        }
        public override Type? ProxyModelType { get; }
        public IEnumerable<ModelMap> SecondaryMaps
        {
            get
            {
                Freeze();
                return _secondaryMaps;
            }
        }
        public override bool UseProxyModel => !typeof(TModel).IsAbstract;

        // Methods.
        public IModelMapsSchema<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer) =>
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
        public IModelMapsSchema<TModel> AddSecondaryMap(
            string id,
            string? baseModelMapId = null,
            Action<BsonClassMap<TModel>>? modelMapInitializer = null,
            IBsonSerializer<TModel>? customSerializer = null) =>
            AddSecondaryMap(new ModelMap<TModel>(
                id,
                new BsonClassMap<TModel>(modelMapInitializer ?? (cm => cm.AutoMap())),
                baseModelMapId,
                customSerializer));

        public IModelMapsSchema<TModel> AddSecondaryMap(ModelMap<TModel> modelMap) =>
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
