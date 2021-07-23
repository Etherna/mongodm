using Etherna.MongODM.Core.Options;

namespace Etherna.MongODM.Core.Tasks
{
    public interface ITaskRunnerBuilder
    {
        void SetMongODMOptions(MongODMOptions options);
    }
}
