namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public abstract class ElementRepresentationBase
    {
        // Constructor.
        public ElementRepresentationBase(IMemberMap memberMap)
        {
            MemberMap = memberMap;
        }

        // Properties.
        public IMemberMap MemberMap { get; }
    }
}
