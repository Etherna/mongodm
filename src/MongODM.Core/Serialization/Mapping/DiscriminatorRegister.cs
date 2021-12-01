using Etherna.MongODM.Core.Conventions;
using Etherna.MongODM.Core.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public class DiscriminatorRegister : FreezableConfig, IDiscriminatorRegister //TODO: Set to work as as FreezableConfig
    {
        // Fields.
        private readonly ReaderWriterLockSlim configLock = new(LockRecursionPolicy.SupportsRecursion);
        private readonly Dictionary<Type, IDiscriminatorConvention> discriminatorConventions = new();
        private readonly Dictionary<BsonValue, HashSet<Type>> discriminators = new();
        private readonly HashSet<Type> discriminatedTypes = new();

        private IDbContext dbContext = default!;

        // Constructor and initializer.
        public void Initialize(IDbContext dbContext)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Instance already initialized");
            this.dbContext = dbContext;

            IsInitialized = true;
        }


        // Properties.
        public bool IsInitialized { get; private set; }

        // Methods.
        public void AddDiscriminator(Type type, BsonValue discriminator)
        {
            // Checks.
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (discriminator is null)
                throw new ArgumentNullException(nameof(discriminator));

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface)
                throw new BsonSerializationException($"Discriminators can only be registered for classes, not for interface {type.FullName}.");

            // Add discriminator.
            configLock.EnterWriteLock();
            try
            {
                if (!discriminators.TryGetValue(discriminator, out HashSet<Type> hashSet))
                {
                    hashSet = new HashSet<Type>();
                    discriminators.Add(discriminator, hashSet);
                }

                if (!hashSet.Contains(type))
                {
                    hashSet.Add(type);

                    //mark all base types as discriminated (so we know that it's worth reading a discriminator)
                    for (var baseType = typeInfo.BaseType; baseType != null; baseType = baseType.GetTypeInfo().BaseType)
                        discriminatedTypes.Add(baseType);
                }
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        public void AddDiscriminatorConvention(Type type, IDiscriminatorConvention convention)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (convention is null)
                throw new ArgumentNullException(nameof(convention));

            configLock.EnterWriteLock();
            try
            {
                if (!discriminatorConventions.ContainsKey(type))
                    discriminatorConventions.Add(type, convention);
                else
                    throw new BsonSerializationException($"There is already a discriminator convention registered for type {type.FullName}.");
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }

        public IDiscriminatorConvention LookupDiscriminatorConvention(Type type)
        {
            configLock.EnterReadLock();
            try
            {
                if (discriminatorConventions.TryGetValue(type, out IDiscriminatorConvention convention))
                    return convention;
            }
            finally
            {
                configLock.ExitReadLock();
            }

            configLock.EnterWriteLock();
            try
            {
                if (!discriminatorConventions.TryGetValue(type, out IDiscriminatorConvention convention))
                {
                    var typeInfo = type.GetTypeInfo();
                    if (type == typeof(object))
                    {
                        //if there is no convention registered for object register the default one
                        convention = new HierarchicalProxyTolerantDiscriminatorConvention("_t", dbContext.ProxyGenerator);
                        AddDiscriminatorConvention(typeof(object), convention);
                    }
                    else if (typeInfo.IsInterface)
                    {
                        // TODO: should convention for interfaces be inherited from parent interfaces?
                        convention = LookupDiscriminatorConvention(typeof(object));
                        AddDiscriminatorConvention(type, convention);
                    }
                    else //type is not typeof(object), or interface
                    {
                        //inherit the discriminator convention from the closest parent that has one
                        convention = LookupDiscriminatorConvention(typeInfo.BaseType);

                        //register the convention for current type
                        AddDiscriminatorConvention(type, convention);
                    }
                }

                return convention;
            }
            finally
            {
                configLock.ExitWriteLock();
            }
        }
    }
}
