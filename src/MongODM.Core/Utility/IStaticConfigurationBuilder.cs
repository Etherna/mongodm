namespace Etherna.MongODM.Utility
{
    /// <summary>
    /// This interface has the scope to inizialize only one time static configurations, when IoC system
    /// has been configured, dependencies can be resolved, and before that any dbcontext starts to operate.
    /// For a proper use, implements it in a class where configuration is invoked by constructor.
    /// So configure it as a singleton on IoC system, and injectit as a dependency for DbContext.
    /// </summary>
    public interface IStaticConfigurationBuilder
    {
    }
}