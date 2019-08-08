using System;

namespace Digicando.MongoDM.Models
{
    public interface IEntityModel : IModel
    {
        DateTime CreationDateTime { get; }

        void DisposeForDelete();
    }

    public interface IEntityModel<TKey> : IEntityModel
    {
        TKey Id { get; }
    }
}
