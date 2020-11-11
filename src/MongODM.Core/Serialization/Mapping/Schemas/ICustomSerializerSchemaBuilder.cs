using System.Diagnostics.CodeAnalysis;

namespace Etherna.MongODM.Core.Serialization.Mapping.Schemas
{
    [SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "A builder can be used in future")]
    public interface ICustomSerializerSchemaBuilder<TModel>
        where TModel : class
    {
    }
}