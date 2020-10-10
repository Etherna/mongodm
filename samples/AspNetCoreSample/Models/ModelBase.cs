using Etherna.MongODM.Core.Models;
using System.Collections.Generic;

namespace Etherna.MongODM.AspNetCoreSample.Models
{
    public abstract class ModelBase : IModel
    {
        public virtual IDictionary<string, object> ExtraElements { get; protected set; }
    }
}
