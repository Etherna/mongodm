using System.Collections.Generic;

namespace Digicando.MongODM.Models
{
    public abstract class ModelBase : IModel
    {
        public virtual IDictionary<string, object>? ExtraElements { get; protected set; }
    }
}