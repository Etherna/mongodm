using Digicando.ExecContext;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Digicando.MongODM.Serialization.Modifiers
{
    class CacheSerializerModifier : IDisposable
    {
        // Consts.
        private const string ModifierKey = "CacheSerializerModifier";

        // Fields.
        private readonly ICollection<CacheSerializerModifier> requestes;

        // Constructors and dispose.
        public CacheSerializerModifier(IContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!context.Items.ContainsKey(ModifierKey))
                context.Items.Add(ModifierKey, new List<CacheSerializerModifier>());

            requestes = context.Items[ModifierKey] as ICollection<CacheSerializerModifier>;

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
        public static bool IsNoCacheEnabled(IContext context)
        {
            if (!context.Items.ContainsKey(ModifierKey))
                return false;
            var requestes = context.Items[ModifierKey] as ICollection<CacheSerializerModifier>;

            lock (((ICollection)requestes).SyncRoot)
                return requestes.Any(r => r.NoCache);
        }
    }
}
