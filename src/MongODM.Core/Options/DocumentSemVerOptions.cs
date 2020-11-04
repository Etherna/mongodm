using Etherna.MongODM.Core.Serialization;

namespace Etherna.MongODM.Core.Options
{
    public class DocumentSemVerOptions
    {
        public SemanticVersion CurrentVersion { get; set; } = "1.0.0";
        public string ElementName { get; set; } = "_v";
        public bool WriteInDocuments { get; set; }
    }
}
