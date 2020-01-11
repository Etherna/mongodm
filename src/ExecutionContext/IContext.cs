using System.Collections.Generic;

namespace Digicando.ExecContext
{
    /// <summary>
    /// Represents an execution context, where information can be put and retrieve alongside
    /// the process with a key-value dictionary.
    /// </summary>
    public interface IContext
    {
        IDictionary<string, object> Items { get; }
    }
}
