using Etherna.ExecContext;
using Etherna.MongoDB.Bson.Serialization;

namespace Etherna.MongODM.Core.Utility
{
    public class SerializationContextAccessor : ISerializationContextAccessor
    {
        // Fields.
        private readonly IExecutionContext executionContext;

        // Constructor.
        public SerializationContextAccessor(IExecutionContext executionContext)
        {
            this.executionContext = executionContext;
        }

        // Method.
        public IBsonSerializerRegistry? TryGetCurrentBsonSerializerRegistry()
        {
            var dbContext = DbExecutionContextHandler.TryGetCurrentDbContext(executionContext);
            return dbContext?.SerializerRegistry;
        }
    }
}
