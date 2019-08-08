using Digicando.MongoDM.Models;
using Digicando.MongoDM.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using System;
using System.Linq.Expressions;

namespace Digicando.MongoDM.Extensions
{
    public static class ClassMapExtensions
    {
        public static BsonMemberMap SetMemberSerializer<TModel, TMember>(
            this BsonClassMap<TModel> classMap,
            Expression<Func<TModel, TMember>> memberLambda,
            IBsonSerializer<TMember> serializer)
        where TMember : class
        {
            var member = classMap.GetMemberMap(memberLambda);
            if (member == null)
                member = classMap.MapMember(memberLambda);
            return member.SetSerializer(serializer);
        }

        public static BsonMemberMap SetMemberSerializer<TModel, TMember, TSerializer, TKey>(
            this BsonClassMap<TModel> classMap,
            Expression<Func<TModel, TMember>> memberLambda,
            ReferenceSerializer<TSerializer, TKey> serializer)
        where TMember : class, TSerializer
        where TSerializer : class, IEntityModel<TKey>
        {
            if (typeof(TMember) == typeof(TSerializer))
                return classMap.SetMemberSerializer(memberLambda, serializer as IBsonSerializer<TMember>);
            else
                return classMap.SetMemberSerializer(memberLambda, serializer.GetAdapter<TMember>());
        }
    }
}
