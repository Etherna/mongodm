using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization.Conventions;
using System;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IDiscriminatorRegistry : IDbContextInitializable
    {
        void AddDiscriminator(Type type, BsonValue discriminator);

        void AddDiscriminatorConvention(Type type, IDiscriminatorConvention convention);

        /// <summary>
        /// Returns whether the given type has any discriminators registered for any of its subclasses.
        /// </summary>
        /// <param name="type">A Type.</param>
        /// <returns>True if the type is discriminated.</returns>
        bool IsTypeDiscriminated(Type type);

        /// <summary>
        /// Looks up the actual type of an object to be deserialized.
        /// </summary>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="discriminator">The discriminator.</param>
        /// <returns>The actual type of the object.</returns>
        Type LookupActualType(Type nominalType, BsonValue? discriminator);

        IDiscriminatorConvention LookupDiscriminatorConvention(Type type);
    }
}