namespace Etherna.MongODM.Migration
{
    public class MigrationResult
    {
        // Constructors.
        private MigrationResult() { }

        // Properties.
        public bool Succeded { get; private set; }
        public long MigratedDocuments { get; private set; }

        // Methods.
        public static MigrationResult Failed() =>
            new MigrationResult
            {
                Succeded = false
            };

        public static MigrationResult Succeeded(long migratedDocuments) =>
            new MigrationResult
            {
                Succeded = true,
                MigratedDocuments = migratedDocuments
            };
    }
}