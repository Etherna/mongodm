using Etherna.MongODM.ProxyModels;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.GeoJsonObjectModel.Serializers;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Etherna.MongODM.Serialization.Serializers
{
    public class GeoPointSerializer<TInModel> : SerializerBase<TInModel>
        where TInModel : class
    {
        // Fields.
        private readonly MemberInfo latitudeMemberInfo;
        private readonly MemberInfo longitudeMemberInfo;
        private readonly GeoJsonPointSerializer<GeoJson2DGeographicCoordinates> pointSerializer;
        private readonly IDbContext dbContext;
        private readonly IProxyGenerator proxyGenerator;

        // Constructors.
        public GeoPointSerializer(
            IDbContext dbContext,
            Expression<Func<TInModel, double>> longitudeMember,
            Expression<Func<TInModel, double>> latitudeMember)
        {
            longitudeMemberInfo = ReflectionHelper.GetMemberInfoFromLambda(longitudeMember);
            latitudeMemberInfo = ReflectionHelper.GetMemberInfoFromLambda(latitudeMember);
            pointSerializer = new GeoJsonPointSerializer<GeoJson2DGeographicCoordinates>();
            this.dbContext = dbContext;
            proxyGenerator = dbContext.ProxyGenerator;
        }

        // Methods.
        public override TInModel Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            // Deserialize point.
            var point = pointSerializer.Deserialize(context, args);
            if (point == null)
            {
                return null!;
            }

            // Create model instance.
            var model = proxyGenerator.CreateInstance<TInModel>(dbContext);

            // Copy data.
            ReflectionHelper.SetValue(model, longitudeMemberInfo, point.Coordinates.Values[0]);
            ReflectionHelper.SetValue(model, latitudeMemberInfo, point.Coordinates.Values[1]);

            return model;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TInModel value)
        {
            // Check null value.
            if (value == null)
            {
                pointSerializer.Serialize(context, args, null);
                return;
            }

            // Create point.
            var coordinate = new GeoJson2DGeographicCoordinates(
                (double)ReflectionHelper.GetValue(value, longitudeMemberInfo)!,
                (double)ReflectionHelper.GetValue(value, latitudeMemberInfo)!);
            var point = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(coordinate);

            // Serialize point.
            pointSerializer.Serialize(context, args, point);
        }
    }
}
