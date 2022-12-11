using Etherna.MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class MemberPath
    {
        // Constructor.
        public MemberPath(IEnumerable<(IModelMap OwnerModel, BsonMemberMap Member)> modelMapsPath)
        {
            if (modelMapsPath is null)
                throw new ArgumentNullException(nameof(modelMapsPath));
            if (!modelMapsPath.Any())
                throw new ArgumentException("Bson path can't be empty", nameof(modelMapsPath));

            ModelMapsPath = modelMapsPath;
        }

        // Properties.
        public IEnumerable<(IModelMap OwnerModel, BsonMemberMap Member)> ModelMapsPath { get; }

        /// <summary>
        /// Description of all encountered entity model classes in member path
        /// </summary>
        public IEnumerable<IModelMap> EntityModelMaps =>
            ModelMapsPath.Select(pair => pair.OwnerModel)
                         .Where(modelMap => modelMap.IsEntity);

        /// <summary>
        /// Typed member path as a string, unique per schema
        /// </summary>
        public string TypedPathAsString => string.Join("|", ModelMapsPath.Select(pair => $"{pair.OwnerModel.ModelType.Name}.{pair.Member.MemberName}"));
    }
}
