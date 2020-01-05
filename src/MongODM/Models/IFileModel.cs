using System.IO;

namespace Digicando.MongODM.Models
{
    public interface IFileModel : IEntityModel<string>
    {
        long Length { get; }
        string Name { get; }
        Stream Stream { get; }
    }
}
