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
using Etherna.MongODM.Core.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public interface IModelMapSchema : IFreezableConfig
    {
        // Properties.
        string Id { get; }
        string? BaseSchemaId { get; }
        BsonClassMap BsonClassMap { get; }
        IEnumerable<IMemberMap> GeneratedMemberMaps { get; }
        IMemberMap? IdMemberMap { get; }
        bool IsCurrentActive { get; }
        bool IsEntity { get; }
        IModelMap ModelMap { get; }
        /// <summary>
        /// ModelMap serializer
        /// </summary>
        IBsonSerializer Serializer { get; }

        // Methods.
        Task<object> FixDeserializedModelAsync(object model);
        void SetBaseModelMapSchema(IModelMapSchema baseModelMapSchema);
        bool TryUseProxyGenerator(IDbContext dbContext);
        void UseProxyGenerator(IDbContext dbContext);
    }

    public interface IModelMapSchema<TModel> : IModelMapSchema
    {
        // Methods.
        Task<TModel> FixDeserializedModelAsync(TModel model);
    }
}