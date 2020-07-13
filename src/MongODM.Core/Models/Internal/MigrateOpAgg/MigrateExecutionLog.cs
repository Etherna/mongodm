using System;

namespace Etherna.MongODM.Models.Internal.MigrateOpAgg
{
    public class MigrateExecutionLog : ModelBase
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
        public MigrateExecutionLog(
            ExecutionState action,
            string migrationId,
            string migrationType,
            long totMigratedDocs)
        {
            Action = action;
            CreationDateTime = DateTime.Now;
            MigrationId = migrationId;
            MigrationType = migrationType;
            TotMigratedDocs = totMigratedDocs;
        }
        protected MigrateExecutionLog() { }

        // Properties.
        public virtual ExecutionState Action { get; protected set; }
        public virtual DateTime CreationDateTime { get; protected set; }
        public virtual string MigrationId { get; protected set; } = default!;
        public string MigrationType { get; } = default!;
        public long TotMigratedDocs { get; protected set; }
    }
}
