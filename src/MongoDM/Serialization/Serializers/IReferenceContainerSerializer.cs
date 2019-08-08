namespace Digicando.MongoDM.Serialization.Serializers
{
    public interface IReferenceContainerSerializer : IClassMapContainerSerializer
    {
        bool? UseCascadeDelete { get; }
    }
}
