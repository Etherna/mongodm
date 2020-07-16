namespace Etherna.MongODM.Models.Internal.DbMigrationOpAgg
{
    class IndexMigrationLog : MigrationLogBase
    {
        // Constructors.
        public IndexMigrationLog(
            string repository,
            ExecutionState state)
            : base(state)
        {
            Repository = repository;
        }
        protected IndexMigrationLog() { }

        // Properties.
        public virtual string Repository { get; protected set; } = default!;
    }
}
