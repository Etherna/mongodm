namespace Etherna.MongODM.Core.Serialization
{
    public interface ICustomSerializerSchemaConfiguration<TModel> : ISchemaConfiguration
        where TModel : class
    {
    }
}