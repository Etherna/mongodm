using System;
using System.Collections.Generic;

namespace Digicando.ExecContext
{
    /// <summary>
    /// A multi context selector that take different contexts, and select the first available.
    /// </summary>
    public class ContextSelector : IContext
    {
        // Fields.
        private readonly IEnumerable<IContext> contexts;

        // Constructors.
        public ContextSelector(
            IEnumerable<IContext> contexts)
        {
            this.contexts = contexts;
        }

        // Proeprties.
        public IDictionary<string, object> Items
        {
            get
            {
                foreach (var context in contexts)
                    if (context.Items != null)
                        return context.Items;
                throw new InvalidOperationException();
            }
        }
    }
}
