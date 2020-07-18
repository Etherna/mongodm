using System;

namespace Etherna.MongODM.Models.Internal.DbMigrationOpAgg
{
    public class DocumentMigrationLog : MigrationLogBase
    {
        // Constructors.
        public DocumentMigrationLog(
            string collectionName,
            string documentMigrationId,
            ExecutionState state,
            long totMigratedDocs)
            : base(state)
        {
            CollectionName = collectionName;
            DocumentMigrationId = documentMigrationId;
            TotMigratedDocs = totMigratedDocs;
        }
        protected DocumentMigrationLog() { }

        // Properties.
        public virtual string CollectionName { get; protected set; } = default!;
        public virtual string DocumentMigrationId { get; protected set; } = default!;
        public virtual long TotMigratedDocs { get; protected set; }
    }
}
