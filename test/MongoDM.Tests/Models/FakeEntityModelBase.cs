using System;

namespace Digicando.MongoDM.Models
{
    public abstract class FakeEntityModelBase<TKey> : ModelBase, IEntityModel<TKey>
    {
        public virtual TKey Id { get; set; }
        public virtual DateTime CreationDateTime { get; private set; }

        public virtual void DisposeForDelete() { }
    }
}
