using MongoDB.Driver.Linq;
using System;
using System.Linq;

namespace Digicando.MongoDM.Extensions
{
    public static class MongoQueryableExtensions
    {
        public static IMongoQueryable<T> Paginate<T>(this IMongoQueryable<T> values, int page, int take)
		{
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page));
            if (take < 0)
                throw new ArgumentOutOfRangeException(nameof(take));

            if (take > 0)
                values = values.Skip(page * take).Take(take);

            return values;
        }
    }
}