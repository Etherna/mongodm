using System.IO;

namespace Digicando.MongoDM.Models
{
    public interface IFileModel : IEntityModel<string>
    {
        long Length { get; }
        string Name { get; }
        Stream Stream { get; }
    }
}
