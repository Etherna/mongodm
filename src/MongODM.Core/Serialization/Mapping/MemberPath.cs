using Etherna.MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class MemberPath
    {
        // Constructor.
        public MemberPath(IEnumerable<(IModelMapSchema OwnerModelMapSchema, BsonMemberMap Member)> modelMapsPath)
        {
            if (modelMapsPath is null)
                throw new ArgumentNullException(nameof(modelMapsPath));

            ModelMapsPath = modelMapsPath;
        }

        // Properties.
        public IEnumerable<(IModelMapSchema OwnerModelMapSchema, BsonMemberMap Member)> ModelMapsPath { get; }

        /// <summary>
        /// Description of all encountered entity model classes in member path
        /// </summary>
        public IEnumerable<IModelMapSchema> EntityModelMaps =>
            ModelMapsPath.Select(pair => pair.OwnerModelMapSchema)
                         .Where(modelMap => modelMap.IsEntity);

        /// <summary>
        /// Typed member path as a string, unique per schema
        /// </summary>
        public string TypedPathAsString => string.Join("|", ModelMapsPath.Select(pair => $"{pair.OwnerModelMapSchema.ModelType.Name}.{pair.Member.MemberName}"));
    }
}
