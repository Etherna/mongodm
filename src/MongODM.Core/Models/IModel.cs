using System.Collections.Generic;

namespace Digicando.MongODM.Models
{
    public interface IModel
    {
        IDictionary<string, object> ExtraElements { get; }
    }
}
