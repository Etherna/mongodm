namespace Etherna.MongODM.Core.Serialization
{
    public interface ICustomSerializerSchemaConfig<TModel> : ISchemaConfig
        where TModel : class
    {
    }
}