// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Serialization.Serializers;
using System;
using System.Globalization;
using System.Reflection;

namespace Etherna.MongODM.Core.Serialization.Providers
{
    public class ModelMapSerializationProvider : BsonSerializationProviderBase
    {
        // Fields.
        private readonly DbContext dbContext;

        // Constructor.
        public ModelMapSerializationProvider(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Methods.
        public override IBsonSerializer? GetSerializer(Type type, IBsonSerializerRegistry serializerRegistry)
        {
            ArgumentNullException.ThrowIfNull(type, nameof(type));

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && typeInfo.ContainsGenericParameters)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Generic type {0} has unassigned type parameters.", BsonUtils.GetFriendlyTypeName(type));
                throw new ArgumentException(message, nameof(type));
            }

            if ((typeInfo.IsClass || (typeInfo.IsValueType && !typeInfo.IsPrimitive)) &&
                !typeof(Array).GetTypeInfo().IsAssignableFrom(type) &&
                !typeof(Enum).GetTypeInfo().IsAssignableFrom(type))
            {
                var modelMapSerializerDefinition = typeof(ModelMapSerializer<>);
                var modelMapSerializerType = modelMapSerializerDefinition.MakeGenericType(type);
                return (IBsonSerializer)Activator.CreateInstance(modelMapSerializerType, dbContext)!;
            }

            return null;
        }
    }
}
