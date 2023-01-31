namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class DocumentElementRepresentation : ElementRepresentationBase
    {
        // Constructor.
        public DocumentElementRepresentation(
            IMemberMap memberMap,
            string? elementName = null) :
            base(memberMap)
        {
            ElementName = elementName;
        }

        // Properties.
        public string? ElementName { get; }
    }
}
