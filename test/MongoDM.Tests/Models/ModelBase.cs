using System.Collections.Generic;

namespace Digicando.MongoDM.Models
{
    public abstract class ModelBase : IModel
    {
        public virtual IDictionary<string, object> ExtraElements { get; protected set; }
    }
}