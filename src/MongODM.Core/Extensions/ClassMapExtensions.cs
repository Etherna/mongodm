//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using System;
using System.Linq.Expressions;

namespace Etherna.MongODM.Core.Extensions
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
