using Etherna.ExecContext;
using Etherna.ExecContext.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Serialization.Modifiers
{
    class ReferenceSerializerModifier : IDisposable
    {
        // Consts.
        private const string ModifierKey = "ReferenceSerializerModifier";

        // Fields.
        private readonly ICollection<ReferenceSerializerModifier> requestes;

        // Constructors and dispose.
        public ReferenceSerializerModifier(IExecutionContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (context.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!context.Items.ContainsKey(ModifierKey))
                context.Items.Add(ModifierKey, new List<ReferenceSerializerModifier>());

            requestes = (ICollection<ReferenceSerializerModifier>)context.Items[ModifierKey];

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
        public static bool IsReadOnlyIdEnabled(IExecutionContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (context.Items is null)
                throw new ExecutionContextNotFoundException();

            if (!context.Items.ContainsKey(ModifierKey))
                return false;
            var requestes = (ICollection<ReferenceSerializerModifier>)context.Items[ModifierKey];

            lock (((ICollection)requestes).SyncRoot)
                return requestes.Any(r => r.ReadOnlyId);
        }
    }
}
