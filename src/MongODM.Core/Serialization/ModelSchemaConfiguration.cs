namespace Etherna.MongODM.Core.Serialization
{
    class ModelSchemaConfiguration<TModel> : IModelSchemaConfiguration<TModel>
        where TModel : class
    {
        // Constructor.
        public ModelSchemaConfiguration(ModelSchema<TModel> activeModelSchema)
        {
            ActiveModelSchema = activeModelSchema;
        }

        // Properties.
        public ModelSchema ActiveModelSchema { get; }

        // Methods.
        public IModelSchemaConfiguration<TModel> AddSecondarySchema(string tbd) => this;
    }
}
