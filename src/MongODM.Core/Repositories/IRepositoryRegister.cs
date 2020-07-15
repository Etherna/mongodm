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

using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Repositories
{
    public interface IRepositoryRegister : IDbContextInitializable
    {
        /// <summary>
        /// Model-Repository map for collection types.
        /// </summary>
        IReadOnlyDictionary<Type, ICollectionRepository> ModelCollectionRepositoryMap { get; }

        /// <summary>
        /// Model-Repository map for gridfs types.
        /// </summary>
        IReadOnlyDictionary<Type, IGridFSRepository> ModelGridFSRepositoryMap { get; }

        /// <summary>
        /// Model-Repository map for both collection and gridfs types.
        /// </summary>
        IReadOnlyDictionary<Type, IRepository> ModelRepositoryMap { get; }
    }
}