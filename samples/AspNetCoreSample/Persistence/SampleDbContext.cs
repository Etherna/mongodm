using Etherna.MongODM.AspNetCoreSample.Models;
using Etherna.MongODM.AspNetCoreSample.Models.ModelMaps;
using Etherna.MongODM.Repositories;
using Etherna.MongODM.Serialization;
using System.Collections.Generic;

namespace Etherna.MongODM.AspNetCoreSample.Persistence
{
    public class SampleDbContext : DbContext, ISampleDbContext
    {
        public SampleDbContext(
            IDbDependencies dependencies,
            DbContextOptions<SampleDbContext> options)
            : base(dependencies, options)
        { }

        public ICollectionRepository<Cat, string> Cats { get; } = new CollectionRepository<Cat, string>("cats");

        protected override IEnumerable<IModelMapsCollector> ModelMapsCollectors =>
            new IModelMapsCollector[]
            {
                new ModelBaseMap(),
                new CatMap()
            };
    }
}
