using System.Collections.Generic;

namespace Etherna.MongODM.Models
{
    public interface IModel
    {
        IDictionary<string, object>? ExtraElements { get; }
    }
}
