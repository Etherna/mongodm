using System.Collections.Generic;

namespace Digicando.ExecContext
{
    public interface ICurrentContextAccessor
    {
        // Properties.
        IReadOnlyDictionary<string, object> Items { get; }
        object SyncRoot { get; }

        // Methods.
        void AddItem(string key, object value);

        bool RemoveItem(string key);
    }
}