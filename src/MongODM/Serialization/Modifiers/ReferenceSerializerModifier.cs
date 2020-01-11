using Digicando.ExecContext;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Digicando.MongODM.Serialization.Modifiers
{
    class ReferenceSerializerModifier : IDisposable
    {
        // Consts.
        private const string ModifierKey = "ReferenceSerializerModifier";

        // Fields.
        private readonly ICollection<ReferenceSerializerModifier> requestes;

        // Constructors and dispose.
        public ReferenceSerializerModifier(IContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!context.Items.ContainsKey(ModifierKey))
                context.Items.Add(ModifierKey, new List<ReferenceSerializerModifier>());

            requestes = context.Items[ModifierKey] as ICollection<ReferenceSerializerModifier>;

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
        public static bool IsReadOnlyIdEnabled(IContext context)
        {
            if (!context.Items.ContainsKey(ModifierKey))
                return false;
            var requestes = context.Items[ModifierKey] as ICollection<ReferenceSerializerModifier>;

            lock (((ICollection)requestes).SyncRoot)
                return requestes.Any(r => r.ReadOnlyId);
        }
    }
}
