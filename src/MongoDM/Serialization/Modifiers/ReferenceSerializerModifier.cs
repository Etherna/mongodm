using Digicando.MongoDM.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Digicando.MongoDM.Serialization.Modifiers
{
    class ReferenceSerializerModifier : IDisposable
    {
        // Consts.
        private const string ModifierKey = "ReferenceSerializerModifier";

        // Fields.
        private readonly ICollection<ReferenceSerializerModifier> requestes;

        // Constructors and dispose.
        public ReferenceSerializerModifier(IContextAccessorFacade contextAccessor)
        {
            if (contextAccessor == null)
                throw new ArgumentNullException(nameof(contextAccessor));

            if (!contextAccessor.Items.ContainsKey(ModifierKey))
                contextAccessor.AddItem(ModifierKey, new List<ReferenceSerializerModifier>());

            requestes = contextAccessor.Items[ModifierKey] as ICollection<ReferenceSerializerModifier>;

            lock (((ICollection)requestes).SyncRoot)
                requestes.Add(this);
        }

        public void Dispose()
        {
            lock (((ICollection)requestes).SyncRoot)
                requestes.Remove(this);
        }

        // Properties.
        public bool ReadOnlyId { get; set; }

        // Static methods.
        public static bool IsReadOnlyIdEnabled(IReadOnlyDictionary<object, object> contextItems)
        {
            if (!contextItems.ContainsKey(ModifierKey))
                return false;
            var requestes = contextItems[ModifierKey] as ICollection<ReferenceSerializerModifier>;

            lock (((ICollection)requestes).SyncRoot)
                return requestes.Any(r => r.ReadOnlyId);
        }
    }
}
