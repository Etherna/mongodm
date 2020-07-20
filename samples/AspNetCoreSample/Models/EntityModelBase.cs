using Etherna.MongODM.Models;
using System;

namespace Etherna.MongODM.AspNetCoreSample.Models
{
    public abstract class EntityModelBase<TKey> : ModelBase, IEntityModel<TKey>
    {
        protected EntityModelBase()
        {
            CreationDateTime = DateTime.Now;
        }

        public virtual TKey Id { get; protected set; }
        public virtual DateTime CreationDateTime { get; protected set; }

        public virtual void DisposeForDelete() { }
    }
}
