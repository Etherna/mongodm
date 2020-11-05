using MongoDB.Bson.IO;
using System;

namespace Etherna.MongODM.Core.Extensions
{
    public static class BsonReaderExtensions
    {
        public static string? IdempotentFindStringElement(this IBsonReader bsonReader, string elementName)
        {
            if (bsonReader is null)
                throw new ArgumentNullException(nameof(bsonReader));
            if (string.IsNullOrEmpty(elementName))
                throw new ArgumentException($"'{nameof(elementName)}' cannot be null or empty", nameof(elementName));

            var bookmark = bsonReader.GetBookmark();
            string? result = bsonReader.FindStringElement(elementName);
            bsonReader.ReturnToBookmark(bookmark);
            return result;
        }
    }
}
