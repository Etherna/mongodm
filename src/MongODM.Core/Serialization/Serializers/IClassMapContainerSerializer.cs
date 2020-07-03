using MongoDB.Bson.Serialization;
using System.Collections.Generic;

namespace Etherna.MongODM.Serialization.Serializers
{
    public interface IClassMapContainerSerializer
    {
        IEnumerable<BsonClassMap> ContainedClassMaps { get; }
    }
}
