using System;

namespace Digicando.MongODM.Serialization.Modifiers
{
    public interface ISerializerModifierAccessor
    {
        bool IsNoCacheEnabled { get; }
        bool IsReadOnlyReferencedIdEnabled { get; }

        IDisposable EnableCacheSerializerModifier(bool noCache);

        IDisposable EnableReferenceSerializerModifier(bool readOnlyId);
    }
}