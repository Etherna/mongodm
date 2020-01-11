using Digicando.ExecContext;
using System;

namespace Digicando.MongODM.Serialization.Modifiers
{
    class SerializerModifierAccessor : ISerializerModifierAccessor
    {
        // Fields.
        private readonly IContext context;

        // Constructors.
        public SerializerModifierAccessor(
            IContext context)
        {
            this.context = context;
        }

        // Properties.
        public bool IsReadOnlyReferencedIdEnabled =>
            ReferenceSerializerModifier.IsReadOnlyIdEnabled(context);

        public bool IsNoCacheEnabled => 
            CacheSerializerModifier.IsNoCacheEnabled(context);

        // Methods.
        public IDisposable EnableCacheSerializerModifier(bool noCache) =>
            new CacheSerializerModifier(context)
            {
                NoCache = noCache
            };

        public IDisposable EnableReferenceSerializerModifier(bool readOnlyId) =>
            new ReferenceSerializerModifier(context)
            {
                ReadOnlyId = readOnlyId
            };
    }
}
