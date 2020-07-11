using Etherna.MongODM.Models;
using Etherna.MongODM.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using System;
using System.Linq.Expressions;

namespace Etherna.MongODM.Extensions
{
    public static class ClassMapExtensions
    {
        public static BsonMemberMap SetMemberSerializer<TModel, TMember>(
            this BsonClassMap<TModel> classMap,
            Expression<Func<TModel, TMember>> memberLambda,
            IBsonSerializer<TMember> serializer)
        where TMember : class
        {
            if (classMap is null)
                throw new ArgumentNullException(nameof(classMap));

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
            if (serializer is null)
                throw new ArgumentNullException(nameof(serializer));

            if (typeof(TMember) == typeof(TSerializer))
                return classMap.SetMemberSerializer(memberLambda, (IBsonSerializer<TMember>)serializer);
            else
                return classMap.SetMemberSerializer(memberLambda, serializer.GetAdapter<TMember>());
        }
    }
}
