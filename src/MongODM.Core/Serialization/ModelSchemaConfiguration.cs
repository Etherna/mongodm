namespace Etherna.MongODM.Core.Serialization
{
    class ModelSchemaConfiguration<TModel> : IModelSchemaConfiguration<TModel>
        where TModel : class
    {
        public IModelSchemaConfiguration<TModel> AddSecondarySchema(string tbd) => this;
    }
}
