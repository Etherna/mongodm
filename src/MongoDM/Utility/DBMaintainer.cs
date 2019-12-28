using Digicando.MongoDM.ProxyModels;
using Digicando.MongoDM.Serialization;
using Digicando.MongoDM.Tasks;
using MongoDB.Driver;
using System.Linq;

namespace Digicando.MongoDM.Utility
{
    public class DBMaintainer : IDBMaintainer
    {
        // Fields.
        private readonly IDocumentSchemaRegister documentSchemaRegister;
        private readonly ITaskRunner taskRunner;

        // Constructors.
        public DBMaintainer(
            IDocumentSchemaRegister documentSchemaRegister,
            ITaskRunner taskRunner)
        {
            this.documentSchemaRegister = documentSchemaRegister;
            this.taskRunner = taskRunner;
        }

        // Methods.
        public void OnUpdatedModel<TKey>(IAuditable updatedModel, TKey modelId)
        {
            var updatedMembers = updatedModel.ChangedMembers;
            var dependencies = updatedMembers.SelectMany(member => documentSchemaRegister.GetMemberDependencies(member))
                                             .Where(d => d.IsEntityReferenceMember);

            foreach (var dependencyGroup in dependencies.GroupBy(d => d.RootModelType))
            {
                var idPaths = dependencyGroup
                    .Select(dependency => string.Join(".", dependency.MemberPathToId.Select(member => member.MemberMap.MemberInfo.Name)))
                    .Distinct();

                // Enqueue call for background job.
                taskRunner.RunUpdateDocDependenciesTask(dependencyGroup.Key, typeof(TKey), idPaths, modelId);
            }
        }
    }
}
