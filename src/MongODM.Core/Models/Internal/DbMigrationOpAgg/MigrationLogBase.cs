using System;

namespace Etherna.MongODM.Models.Internal.DbMigrationOpAgg
{
    public abstract class MigrationLogBase : ModelBase
    {
        // Enums.
        public enum ExecutionState
        {
            Executing,
            Succeded,
            Skipped,
            Failed
        }

        // Constructors.
        public MigrationLogBase(ExecutionState state)
        {
            CreationDateTime = DateTime.Now;
            State = state;
        }
        protected MigrationLogBase() { }

        // Properties.
        public virtual ExecutionState State { get; protected set; }
        public virtual DateTime CreationDateTime { get; protected set; }
    }
}
