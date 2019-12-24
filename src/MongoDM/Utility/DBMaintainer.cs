using Digicando.MongoDM.ProxyModels;
using Digicando.MongoDM.Serialization;
using Digicando.MongoDM.Tasks;
using Hangfire;
using MongoDB.Driver;
using System.Linq;

namespace Digicando.MongoDM.Utility
{
    public class DBMaintainer : IDBMaintainer
    {
        // Fields.
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IDocumentSchemaRegister documentSchemaRegister;

        // Constructors.
        public DBMaintainer(
            IBackgroundJobClient backgroundJobClient,
            IDocumentSchemaRegister documentSchemaRegister)
        {
            this.backgroundJobClient = backgroundJobClient;
            this.documentSchemaRegister = documentSchemaRegister;
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
                backgroundJobClient.Enqueue<IUpdateDocDependenciesTask>(
                    task => task.Run_NOTGENERICPROXY_Async(null, dependencyGroup.Key, typeof(TKey), idPaths, modelId));
            }
        }
    }
}
