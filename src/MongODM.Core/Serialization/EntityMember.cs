using MongoDB.Bson.Serialization;

namespace Etherna.MongODM.Serialization
{
    public class EntityMember
    {
        // Constructors.
        public EntityMember(
            BsonClassMap? entityClassMap,
            BsonMemberMap memberMap)
        {
            EntityClassMap = entityClassMap;
            MemberMap = memberMap;
        }

        // Properties.
        public BsonClassMap? EntityClassMap { get; }
        public bool IsId => MemberMap == EntityClassMap?.IdMemberMap;
        public BsonMemberMap MemberMap { get; }
    }
}
