using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Digicando.MongoDM.Serialization.Serializers
{
    public interface IClassMapContainerSerializer
    {
        IEnumerable<BsonClassMap> ContainedClassMaps { get; }
    }
}
