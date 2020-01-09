using System.Collections.Generic;

namespace Digicando.ExecContext
{
    public interface IContextAccessor
    {
        IDictionary<string, object> Items { get; }
    }
}
