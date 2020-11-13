using MongoDB.Bson;
using MongoDB.Bson.IO;
using System;

namespace Etherna.MongODM.Core.Extensions
{
    public static class BsonReaderExtensions
    {
        public static string? FindStringElementInDocument(this IBsonReader bsonReader, string elementName)
        {
            if (bsonReader is null)
                throw new ArgumentNullException(nameof(bsonReader));
            if (string.IsNullOrEmpty(elementName))
                throw new ArgumentException($"'{nameof(elementName)}' cannot be null or empty", nameof(elementName));

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType != BsonType.Document)
                return null;

            var bookmark = bsonReader.GetBookmark();
            bsonReader.ReadStartDocument();

            string? result = bsonReader.FindStringElement(elementName);

            bsonReader.ReturnToBookmark(bookmark);
            return result;
        }
    }
}
