using System.Collections.Generic;

namespace Digicando.MongoDM.Models
{
    public interface IModel
    {
        IDictionary<string, object> ExtraElements { get; }
    }
}
