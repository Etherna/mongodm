using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Tasks;
using MongoDB.Driver;
using System;
using System.Linq;

namespace Etherna.MongODM.Utility
{
    public class DbContextMaintainer : IDbContextMaintainer
    {
        // Fields.
        private IDbContext dbContext = default!;
        private readonly ITaskRunner taskRunner;

        // Constructors and initialization.
        public DbContextMaintainer(ITaskRunner taskRunner)
        {
            this.taskRunner = taskRunner;
        }

        public void Initialize(IDbContext dbContext)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");

            this.dbContext = dbContext;

            IsInitialized = true;
        }

        // Properties.
        public bool IsInitialized { get; private set; }

        // Methods.
        public void OnUpdatedModel<TKey>(IAuditable updatedModel, TKey modelId)
        {
            if (updatedModel is null)
                throw new ArgumentNullException(nameof(updatedModel));
            if (modelId is null)
                throw new ArgumentNullException(nameof(modelId));

            var updatedMembers = updatedModel.ChangedMembers;
            var dependencies = updatedMembers.SelectMany(member => dbContext.DocumentSchemaRegister.GetMemberDependencies(member))
                                             .Where(d => d.IsEntityReferenceMember);

            foreach (var dependencyGroup in dependencies.GroupBy(d => d.RootModelType))
            {
                var idPaths = dependencyGroup
                    .Select(dependency => string.Join(".", dependency.MemberPathToId.Select(member => member.MemberMap.MemberInfo.Name)))
                    .Distinct();

                // Enqueue call for background job.
                taskRunner.RunUpdateDocDependenciesTask(dbContext.GetType(), dependencyGroup.Key, typeof(TKey), idPaths, modelId);
            }
        }
    }
}
