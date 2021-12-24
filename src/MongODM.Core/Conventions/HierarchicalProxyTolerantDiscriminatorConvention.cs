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

using Etherna.ExecContext;
using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.IO;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongoDB.Bson.Serialization.Conventions;
using Etherna.MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Conventions
{
    public class HierarchicalProxyTolerantDiscriminatorConvention : IDiscriminatorConvention
    {
        // Fields.
        private readonly IDbContext? _dbContext; //remove nullability with constructors that don't ask it, when will be possible
        private readonly IExecutionContext? executionContext;

        // Constructors.
        public HierarchicalProxyTolerantDiscriminatorConvention(
            IDbContext dbContext,
            string elementName)
        {
            _dbContext = dbContext;

            ElementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
            if (elementName.IndexOf('\0') != -1)
                throw new ArgumentException("Element names cannot contain nulls.", nameof(elementName));
        }

        /// <summary>
        /// Only needed for static registration on <see cref="object"/>, used when dbcontext is not available.
        /// Remove when <see cref="BsonSerializer.LookupDiscriminatorConvention(Type)"/> static call will be removed.
        /// </summary>
        /// <param name="elementName">Discriminator element name</param>
        public HierarchicalProxyTolerantDiscriminatorConvention(
            string elementName,
            IExecutionContext executionContext)
        {
            ElementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
            if (elementName.IndexOf('\0') != -1)
                throw new ArgumentException("Element names cannot contain nulls.", nameof(elementName));

            this.executionContext = executionContext;
        }

        public IDbContext DbContext
        {
            get
            {
                if (_dbContext is not null)
                    return _dbContext;

                /* If we didn't injected a dbContext, this is an instance retrieved from a static invoke.
                 * Try to find it from execution contenxt. */
                if (executionContext is null)
                    throw new InvalidOperationException();

                return Core.DbContext.GetCurrentDbContext(executionContext);
            }
        }
        public string ElementName { get; }

        // Methods.
        public Type GetActualType(IBsonReader bsonReader, Type nominalType)
        {
            if (bsonReader is null)
                throw new ArgumentNullException(nameof(bsonReader));

            //the BsonReader is sitting at the value whose actual type needs to be found
            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Document)
            {
                //we can skip looking for a discriminator if nominalType has no discriminated sub types
                if (DbContext.DiscriminatorRegistry.IsTypeDiscriminated(nominalType))
                {
                    var bookmark = bsonReader.GetBookmark();
                    bsonReader.ReadStartDocument();
                    var actualType = nominalType;
                    if (bsonReader.FindElement(ElementName))
                    {
                        var context = BsonDeserializationContext.CreateRoot(bsonReader);
                        var discriminator = BsonValueSerializer.Instance.Deserialize(context);
                        if (discriminator.IsBsonArray)
                        {
                            discriminator = discriminator.AsBsonArray.Last(); //last item is leaf class discriminator
                        }
                        actualType = DbContext.DiscriminatorRegistry.LookupActualType(nominalType, discriminator);
                    }
                    bsonReader.ReturnToBookmark(bookmark);
                    return actualType;
                }
            }

            return nominalType;
        }

        /// <summary>
        /// Gets the discriminator value for an actual type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <returns>The discriminator value.</returns>
        public BsonValue? GetDiscriminator(Type nominalType, Type actualType)
        {
            // Remove proxy type.
            actualType = DbContext.ProxyGenerator.PurgeProxyType(actualType);

            // Find active class map for model type.
            var classMap = DbContext.SchemaRegistry.GetActiveClassMap(actualType);

            // Get discriminator from class map.
            if (actualType != nominalType || classMap.DiscriminatorIsRequired || classMap.HasRootClass)
            {
                if (classMap.HasRootClass && !classMap.IsRootClass)
                {
                    var values = new List<BsonValue>();
                    for (; !classMap.IsRootClass; classMap = classMap.BaseClassMap)
                    {
                        values.Add(classMap.Discriminator);
                    }
                    values.Add(classMap.Discriminator); //add the root class's discriminator
                    return new BsonArray(values.Reverse<BsonValue>()); //reverse to put leaf class last
                }
                else
                    return classMap.Discriminator;
            }

            return null;
        }
    }
}
