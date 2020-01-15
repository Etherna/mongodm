using Digicando.MongODM.Serialization;

namespace Digicando.MongODM
{
    public class DbContextOptions
    {
        public string ConnectionString { get; set; } = "mongodb://localhost/localDb";
        public string DBName { get; set; } = "localDb";
        public DocumentVersion DocumentVersion { get; set; } = "1.0.0";
    }
}
