using Digicando.MongODM.Utility;
using System;

namespace Digicando.MongODM.Serialization.Modifiers
{
    class SerializerModifierAccessor : ISerializerModifierAccessor
    {
        // Fields.
        private readonly IContextAccessorFacade contextAccessorFacade;

        // Constructors.
        public SerializerModifierAccessor(
            IContextAccessorFacade contextAccessorFacade)
        {
            this.contextAccessorFacade = contextAccessorFacade;
        }

        // Properties.
        public bool IsReadOnlyReferencedIdEnabled =>
            ReferenceSerializerModifier.IsReadOnlyIdEnabled(contextAccessorFacade.Items);

        public bool IsNoCacheEnabled => 
            CacheSerializerModifier.IsNoCacheEnabled(contextAccessorFacade.Items);

        // Methods.
        public IDisposable EnableCacheSerializerModifier(bool noCache) =>
            new CacheSerializerModifier(contextAccessorFacade)
            {
                NoCache = noCache
            };

        public IDisposable EnableReferenceSerializerModifier(bool readOnlyId) =>
            new ReferenceSerializerModifier(contextAccessorFacade)
            {
                ReadOnlyId = readOnlyId
            };
    }
}
