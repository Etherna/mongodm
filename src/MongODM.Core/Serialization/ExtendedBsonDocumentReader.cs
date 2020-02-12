using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Digicando.MongODM.Serialization
{
    public class ExtendedBsonDocumentReader : BsonDocumentReader
    {
        public ExtendedBsonDocumentReader(BsonDocument document)
            : base(document)
        { }

        public DocumentVersion DocumentVersion { get; set; }
    }
}
