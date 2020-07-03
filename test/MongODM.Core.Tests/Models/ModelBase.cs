using System.Collections.Generic;

namespace Etherna.MongODM.Models
{
    public abstract class ModelBase : IModel
    {
        public virtual IDictionary<string, object>? ExtraElements { get; protected set; }
    }
}