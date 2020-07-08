using Etherna.MongODM.ProxyModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using System;

namespace Etherna.MongODM.Conventions
{
    public class HierarchicalProxyTolerantDiscriminatorConvention : HierarchicalDiscriminatorConvention
    {
        // Fields.
        private readonly IProxyGenerator proxyGenerator;

        // Constructors.
        public HierarchicalProxyTolerantDiscriminatorConvention(
            string elementName,
            IProxyGenerator proxyGenerator)
            : base(elementName)
        {
            this.proxyGenerator = proxyGenerator ?? throw new ArgumentNullException(nameof(proxyGenerator));
        }

        // Methods.
        public override BsonValue GetDiscriminator(Type nominalType, Type actualType) =>
            base.GetDiscriminator(nominalType, proxyGenerator.PurgeProxyType(actualType));
    }
}
