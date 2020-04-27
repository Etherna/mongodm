using System;
using System.Collections.Generic;

namespace Digicando.ExecContext
{
    /// <summary>
    ///     A multi context selector that take different contexts, and select the first available.
    /// </summary>
    /// <remarks>
    ///     This class is intended to have the same lifetime of it's consumer. For example, in case
    ///     of using with a DbContext, the same DbContext instance will use the same ContextSelector
    ///     instance. This mean that if a DbContext is running over different execution contexts,
    ///     every <see cref="Items"/> invoke on same context needs to return the same dictionary.
    ///     The simplest way to perform this, is to return the first not null available dictionary
    ///     on subscribed contexts.
    /// </remarks>
    public class ExecutionContextSelector : IExecutionContext
    {
        // Fields.
        private readonly IEnumerable<IExecutionContext> contexts;

        // Constructors.
        public ExecutionContextSelector(IEnumerable<IExecutionContext> contexts)
        {
            this.contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        }

        // Proeprties.
        public IDictionary<object, object>? Items
        {
            get
            {
                foreach (var context in contexts)
                    if (context.Items != null)
                        return context.Items;
                return null;
            }
        }
    }
}
