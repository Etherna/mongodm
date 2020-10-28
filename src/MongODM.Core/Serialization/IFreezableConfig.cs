namespace Etherna.MongODM.Core.Serialization
{
    public interface IFreezableConfig
    {
        // Properties.
        public bool IsFrozen { get; }

        // Methods.
        void Freeze();
    }
}