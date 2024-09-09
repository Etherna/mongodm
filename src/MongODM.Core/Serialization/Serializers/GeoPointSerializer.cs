﻿// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Bson.Serialization.Serializers;
using Etherna.MongoDB.Driver.GeoJsonObjectModel;
using Etherna.MongoDB.Driver.GeoJsonObjectModel.Serializers;
using Etherna.MongODM.Core.ProxyModels;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Serializers
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
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
