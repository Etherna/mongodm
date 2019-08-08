using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Digicando.MongoDM.Serialization
{
    public class ExtendedBsonDocumentWriter : BsonDocumentWriter
    {
        public ExtendedBsonDocumentWriter(BsonDocument document)
            : base(document)
        { }

        public bool IsRootDocument { get; set; }
    }
}
