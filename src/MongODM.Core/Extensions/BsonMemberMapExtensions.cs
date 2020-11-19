using MongoDB.Bson.Serialization;
using System;

namespace Etherna.MongODM.Core.Extensions
{
    public static class BsonMemberMapExtensions
    {
        public static bool IsIdMember(this BsonMemberMap memberMap)
        {
            if (memberMap is null)
                throw new ArgumentNullException(nameof(memberMap));

            return memberMap.ClassMap.IdMemberMap == memberMap;
        }
    }
}
