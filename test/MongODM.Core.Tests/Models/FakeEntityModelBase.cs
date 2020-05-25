using System;

namespace Etherna.MongODM.Models
{
    public abstract class FakeEntityModelBase<TKey> : ModelBase, IEntityModel<TKey>
    {
        public virtual TKey Id { get; set; } = default!;
        public virtual DateTime CreationDateTime { get; private set; }

        public virtual void DisposeForDelete() { }
    }
}
