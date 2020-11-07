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
        private IDictionary<string, ModelMap> _allMapsDictionary = default!;
        private readonly List<ModelMap> _secondaryMaps = new List<ModelMap>();
        private readonly IDbContext dbContext;

        // Constructor.
        public ModelMapsSchema(
            ModelMap<TModel> activeMap,
            IDbContext dbContext)
            : base(typeof(TModel))
        {
            ActiveMap = activeMap ?? throw new ArgumentNullException(nameof(activeMap));
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Verify if have to use proxy model.
            if (!typeof(TModel).IsAbstract)
            {
                ProxyModelType = dbContext.ProxyGenerator.CreateInstance(ModelType, dbContext).GetType();
                ActiveMap.UseProxyGenerator(dbContext);
            }

            // Verify if needs to use default serializer.
            if (!typeof(TModel).IsAbstract && activeMap.Serializer is null)
                ActiveMap.UseDefaultSerializer(dbContext);
        }

        // Properties.
        public ModelMap ActiveMap { get; }
        public override IBsonSerializer? ActiveSerializer => ActiveMap.Serializer;
        public IDictionary<string, ModelMap> AllMapsDictionary
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

        public IBsonSerializer<TModel>? FallbackSerializer { get; private set; }
        public override Type? ProxyModelType { get; }
        public IEnumerable<ModelMap> SecondaryMaps => _secondaryMaps;

        // Methods.
        public IModelMapsSchema<TModel> AddFallbackCustomSerializer(IBsonSerializer<TModel> fallbackSerializer) =>
            ExecuteConfigAction(() =>
            {
                if (fallbackSerializer is null)
                    throw new ArgumentNullException(nameof(fallbackSerializer));
                if (FallbackSerializer != null)
                    throw new InvalidOperationException("Fallback serializer already setted");

                FallbackSerializer = fallbackSerializer;

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

                // Verify if this schema uses proxy model.
                if (ProxyModelType != null)
                    modelMap.UseProxyGenerator(dbContext);

                // Add schema.
                _secondaryMaps.Add(modelMap);
                return this;
            });

        // Protected methods.
        protected override void FreezeAction()
        {
            ActiveMap.Freeze();
            foreach (var secondaryMap in _secondaryMaps)
                secondaryMap.Freeze();
        }
    }
}
