using Digicando.MongoDM.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Digicando.MongoDM.Serialization.Modifiers
{
    class CacheSerializerModifier : IDisposable
    {
        // Consts.
        private const string ModifierKey = "CacheSerializerModifier";

        // Fields.
        private readonly ICollection<CacheSerializerModifier> requestes;

        // Constructors and dispose.
        public CacheSerializerModifier(IContextAccessorFacade contextAccessor)
        {
            if (contextAccessor == null)
                throw new ArgumentNullException(nameof(contextAccessor));

            if (!contextAccessor.Items.ContainsKey(ModifierKey))
                contextAccessor.AddItem(ModifierKey, new List<CacheSerializerModifier>());

            requestes = contextAccessor.Items[ModifierKey] as ICollection<CacheSerializerModifier>;

            lock (((ICollection)requestes).SyncRoot)
                requestes.Add(this);
        }

        public void Dispose()
        {
            lock (((ICollection)requestes).SyncRoot)
                requestes.Remove(this);
        }

        // Properties.
        public bool NoCache { get; set; }

        // Static methods.
        public static bool IsNoCacheEnabled(IReadOnlyDictionary<object, object> contextItems)
        {
            if (!contextItems.ContainsKey(ModifierKey))
                return false;
            var requestes = contextItems[ModifierKey] as ICollection<CacheSerializerModifier>;

            lock (((ICollection)requestes).SyncRoot)
                return requestes.Any(r => r.NoCache);
        }
    }
}
