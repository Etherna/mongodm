using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IDiscriminatorRegister : IDbContextInitializable
    {
        void AddDiscriminator(Type type, BsonValue discriminator);
        void AddDiscriminatorConvention(Type type, IDiscriminatorConvention convention);
        IDiscriminatorConvention LookupDiscriminatorConvention(Type type);
    }
}