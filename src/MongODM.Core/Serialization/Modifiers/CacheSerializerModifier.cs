using Digicando.ExecContext;
using Digicando.ExecContext.Exceptions;
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
        public CacheSerializerModifier(IExecutionContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (context.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!context.Items.ContainsKey(ModifierKey))
                context.Items.Add(ModifierKey, new List<CacheSerializerModifier>());

            requestes = (ICollection<CacheSerializerModifier>)context.Items[ModifierKey];

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
        public static bool IsNoCacheEnabled(IExecutionContext context)
        {
            if (context.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!context.Items.ContainsKey(ModifierKey))
                return false;
            var requestes = (ICollection<CacheSerializerModifier>)context.Items[ModifierKey];

            lock (((ICollection)requestes).SyncRoot)
                return requestes.Any(r => r.NoCache);
        }
    }
}
