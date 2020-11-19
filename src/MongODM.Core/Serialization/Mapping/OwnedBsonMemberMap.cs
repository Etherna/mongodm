using MongoDB.Bson.Serialization;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public record OwnedBsonMemberMap
    {
        public OwnedBsonMemberMap(BsonClassMap ownerClass, BsonMemberMap member) =>
            (OwnerClass, Member) = (ownerClass, member);

        public BsonClassMap OwnerClass { get; }
        public BsonMemberMap Member { get; }
    }
}
