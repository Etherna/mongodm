using System.IO;

namespace Etherna.MongODM.Models
{
    public interface IFileModel : IEntityModel<string>
    {
        long Length { get; }
        string Name { get; }
        Stream Stream { get; }
    }
}
