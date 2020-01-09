using System;
using System.Collections.Generic;
using System.Linq;

namespace Digicando.ExecContext
{
    public class CurrentContextAccessor : ICurrentContextAccessor
    {
        // Fields.
        private readonly IEnumerable<IContextAccessor> contextAccessors;

        // Constructors.
        public CurrentContextAccessor(
            IEnumerable<IContextAccessor> contextAccessors)
        {
            this.contextAccessors = contextAccessors;
        }

        // Proeprties.
        public IReadOnlyDictionary<string, object> Items =>
            CurrentContextItems.ToDictionary(pair => pair.Key, pair => pair.Value);

        public object SyncRoot => CurrentContextItems;

        // Private properties.
        private IDictionary<string, object> CurrentContextItems
        {
            get
            {
                foreach (var accessor in contextAccessors)
                    if (accessor.Items != null)
                        return accessor.Items;
                throw new InvalidOperationException();
            }
        }

        // Methods.
        public void AddItem(string key, object value) =>
            CurrentContextItems.Add(key, value);

        public bool RemoveItem(string key) =>
            CurrentContextItems.Remove(key);
    }
}
