using Digicando.MongODM.Serialization;

namespace Digicando.MongODM
{
    public class DbContextOptions
    {
        public DbContextOptions(
            string connectionString,
            string dbName,
            DocumentVersion documentVersion = null)
        {
            ConnectionString = connectionString;
            DBName = dbName;
            DocumentVersion = documentVersion ?? "1.0.0";
        }

        public string ConnectionString { get; }
        public string DBName { get; }
        public DocumentVersion DocumentVersion { get; }
    }
}
