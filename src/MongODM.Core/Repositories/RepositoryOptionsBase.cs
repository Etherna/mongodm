namespace Digicando.MongODM.Repositories
{
    public abstract class RepositoryOptionsBase
    {
        public RepositoryOptionsBase(string name)
        {
            Name = name ?? throw new System.ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }
}
