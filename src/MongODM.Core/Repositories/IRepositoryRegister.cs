using System;
using System.Collections.Generic;

#nullable enable
namespace Digicando.MongODM.Repositories
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