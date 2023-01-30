namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class ArrayElementRepresentation : ElementRepresentationBase
    {
        // Constructor.
        public ArrayElementRepresentation(
            IMemberMap memberMap,
            int? itemIndex = null) :
            base(memberMap)
        {
            ItemIndex = itemIndex;
        }

        // Properties.
        public int? ItemIndex { get; }
    }
}
