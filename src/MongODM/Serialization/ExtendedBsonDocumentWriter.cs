using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace Digicando.MongODM.Serialization
{
    public class ExtendedBsonDocumentWriter : BsonDocumentWriter
    {
        public ExtendedBsonDocumentWriter(BsonDocument document)
            : base(document)
        { }

        public bool IsRootDocument { get; set; }
    }
}
