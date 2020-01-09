using Digicando.ExecContext;
using System;

namespace Digicando.MongODM.Serialization.Modifiers
{
    class SerializerModifierAccessor : ISerializerModifierAccessor
    {
        // Fields.
        private readonly ICurrentContextAccessor contextAccessorAccessor;

        // Constructors.
        public SerializerModifierAccessor(
            ICurrentContextAccessor contextAccessorAccessor)
        {
            this.contextAccessorAccessor = contextAccessorAccessor;
        }

        // Properties.
        public bool IsReadOnlyReferencedIdEnabled =>
            ReferenceSerializerModifier.IsReadOnlyIdEnabled(contextAccessorAccessor.Items);

        public bool IsNoCacheEnabled => 
            CacheSerializerModifier.IsNoCacheEnabled(contextAccessorAccessor.Items);

        // Methods.
        public IDisposable EnableCacheSerializerModifier(bool noCache) =>
            new CacheSerializerModifier(contextAccessorAccessor)
            {
                NoCache = noCache
            };

        public IDisposable EnableReferenceSerializerModifier(bool readOnlyId) =>
            new ReferenceSerializerModifier(contextAccessorAccessor)
            {
                ReadOnlyId = readOnlyId
            };
    }
}
