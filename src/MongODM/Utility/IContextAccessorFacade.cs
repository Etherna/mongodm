using System.Collections.Generic;

namespace Digicando.MongODM.Utility
{
    public interface IContextAccessorFacade
    {
        // Properties.
        IReadOnlyDictionary<object, object> Items { get; }
        object SyncRoot { get; }

        // Methods.
        void AddItem(string key, object value);

        bool RemoveItem(string key);
    }
}