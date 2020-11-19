namespace Etherna.MongODM.Core.Utility
{
    public interface IFreezableConfig
    {
        // Properties.
        public bool IsFrozen { get; }

        // Methods.
        void Freeze();
    }
}