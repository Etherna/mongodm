using Etherna.MongODM.Core.Serialization;

namespace Etherna.MongODM.Core.Options
{
    public class ApplicationVersionOptions
    {
        public SemanticVersion CurrentVersion { get; set; } = "1.0.0";
        public string ElementName { get; set; } = "v";
        public bool WriteInDocuments { get; set; }
    }
}
