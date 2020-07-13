using Etherna.MongODM.Conventions;
using Etherna.MongODM.Models.Internal;
using Etherna.MongODM.ProxyModels;
using Etherna.MongODM.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Etherna.MongODM.AspNetCore
{
    public class StaticConfigurationBuilder<TModelBase> : IStaticConfigurationBuilder
    {
        public StaticConfigurationBuilder(IProxyGenerator proxyGenerator)
        {
            // Register conventions.
            ConventionRegistry.Register("Enum string", new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            }, c => true);

            BsonSerializer.RegisterDiscriminatorConvention(typeof(TModelBase),
                new HierarchicalProxyTolerantDiscriminatorConvention("_t", proxyGenerator));
            BsonSerializer.RegisterDiscriminatorConvention(typeof(EntityModelBase),
                new HierarchicalProxyTolerantDiscriminatorConvention("_t", proxyGenerator));
        }
    }
}
