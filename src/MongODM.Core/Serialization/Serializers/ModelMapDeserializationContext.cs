namespace Etherna.MongODM.Core.Serialization.Serializers
{
    public class ModelMapDeserializationContext
    {
        // Constructor.
        public ModelMapDeserializationContext(
            string? modelMapId,
            SemanticVersion? semVer)
        {
            ModelMapId = modelMapId;
            SemVer = semVer;
        }

        // Properties.
        public string? ModelMapId { get; }
        public SemanticVersion? SemVer { get; }
    }
}
