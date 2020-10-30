namespace Etherna.MongODM.Core.Serialization.Schemas
{
    public interface IFreezableConfig
    {
        // Properties.
        public bool IsFrozen { get; }

        // Methods.
        void Freeze();
    }
}