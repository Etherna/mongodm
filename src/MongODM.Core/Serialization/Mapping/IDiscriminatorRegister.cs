using MongoDB.Bson;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IDiscriminatorRegister
    {
        void AddDiscriminator(Type type, BsonValue discriminator);
    }
}