using Digicando.ExecContext;
using System;

namespace Digicando.MongODM.Serialization.Modifiers
{
    public class SerializerModifierAccessor : ISerializerModifierAccessor
    {
        // Fields.
        private readonly IExecutionContext executionContext;

        // Constructors.
        public SerializerModifierAccessor(
            IExecutionContext executionContext)
        {
            this.executionContext = executionContext;
        }

        // Properties.
        public bool IsReadOnlyReferencedIdEnabled =>
            ReferenceSerializerModifier.IsReadOnlyIdEnabled(executionContext);

        public bool IsNoCacheEnabled => 
            CacheSerializerModifier.IsNoCacheEnabled(executionContext);

        // Methods.
        public IDisposable EnableCacheSerializerModifier(bool noCache) =>
            new CacheSerializerModifier(executionContext)
            {
                NoCache = noCache
            };

        public IDisposable EnableReferenceSerializerModifier(bool readOnlyId) =>
            new ReferenceSerializerModifier(executionContext)
            {
                ReadOnlyId = readOnlyId
            };
    }
}
