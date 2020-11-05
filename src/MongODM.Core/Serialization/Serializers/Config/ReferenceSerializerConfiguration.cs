using Etherna.MongODM.Core.Utility;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Etherna.MongODM.Core.Serialization.Serializers.Config
{
    public class ReferenceSerializerConfiguration : FreezableConfig
    {
        // Fields.
        private bool _useCascadeDelete;
        private readonly IDbContext dbContext;
        private readonly Dictionary<Type, IReferenceSchema> _schemas = new Dictionary<Type, IReferenceSchema>();

        // Constructor.
        public ReferenceSerializerConfiguration(IDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Properties.
        public IReadOnlyDictionary<Type, IReferenceSchema> Schemas
        {
            get
            {
                Freeze();
                return _schemas;
            }
        }

        public bool UseCascadeDelete
        {
            get => _useCascadeDelete;
            set => ExecuteConfigAction(() => _useCascadeDelete = value);
        }

        // Methods.
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Don't need to dispose")]
        public IReferenceSchema<TModel> AddModelMapSchema<TModel>(
            string activeModelMapId,
            Action<BsonClassMap<TModel>>? activeModelMapInitializer = null)
            where TModel : class =>
            AddModelMapSchema(new ReferenceModelMap<TModel>(
                activeModelMapId,
                new BsonClassMap<TModel>(activeModelMapInitializer ?? (cm => cm.AutoMap()))));

        public IReferenceSchema<TModel> AddModelMapSchema<TModel>(
            ReferenceModelMap<TModel> activeModelMap)
            where TModel : class =>
            ExecuteConfigAction(() =>
            {
                if (activeModelMap is null)
                    throw new ArgumentNullException(nameof(activeModelMap));

                // Register and return schema configuration.
                var modelSchemaConfiguration = new ReferenceSchema<TModel>(activeModelMap, dbContext);
                _schemas.Add(typeof(TModel), modelSchemaConfiguration);

                return modelSchemaConfiguration;
            });

        public IReferenceSchema<TModel> GetModelMapsSchema<TModel>()
            where TModel : class =>
            (IReferenceSchema<TModel>)Schemas[typeof(TModel)];

        // Protected methods.
        protected override void FreezeAction()
        {
            foreach (var schema in _schemas.Values)
                schema.Freeze();
        }
    }
}
