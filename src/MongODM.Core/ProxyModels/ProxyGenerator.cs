using Castle.DynamicProxy;
using Etherna.MongODM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Etherna.MongODM.ProxyModels
{
    public class ProxyGenerator : IProxyGenerator
    {
        // Fields.
        private readonly Castle.DynamicProxy.IProxyGenerator proxyGeneratorCore;

        private readonly Dictionary<Type,
            (Type[] AdditionalInterfaces, Func<IDbContext, IInterceptor[]> InterceptorInstancerSelector)> modelConfigurationDictionary =
            new Dictionary<Type, (Type[] AdditionalInterfaces, Func<IDbContext, IInterceptor[]> InterceptorInstancerSelector)>();
        private readonly ReaderWriterLockSlim modelConfigurationDictionaryLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private readonly Dictionary<Type, Type> proxyTypeDictionary = new Dictionary<Type, Type>();
        private readonly ReaderWriterLockSlim proxyTypeDictionaryLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        // Constructors.
        public ProxyGenerator(
            Castle.DynamicProxy.IProxyGenerator proxyGeneratorCore)
        {
            this.proxyGeneratorCore = proxyGeneratorCore;
        }

        // Methods.
        public object CreateInstance(
            IDbContext dbContext,
            Type type,
            params object[] constructorArguments)
        {
            // Get configuration.
            (Type[] AdditionalInterfaces, Func<IDbContext, IInterceptor[]> InterceptorInstancerSelector) configuration = (null!, null!);
            modelConfigurationDictionaryLock.EnterReadLock();
            bool configurationFound = false;
            try
            {
                if (modelConfigurationDictionary.ContainsKey(type))
                {
                    configuration = modelConfigurationDictionary[type];
                    configurationFound = true;
                }
            }
            finally
            {
                modelConfigurationDictionaryLock.ExitReadLock();
            }

            if (!configurationFound)
            {
                modelConfigurationDictionaryLock.EnterWriteLock();
                try
                {
                    if (modelConfigurationDictionary.ContainsKey(type))
                    {
                        configuration = modelConfigurationDictionary[type];
                    }
                    else
                    {
                        var additionalInterfaces = GetAdditionalInterfaces(type);
                        configuration = (additionalInterfaces, GetInterceptorInstancer(type, additionalInterfaces));
                        modelConfigurationDictionary.Add(type, configuration);
                    }
                }
                finally
                {
                    modelConfigurationDictionaryLock.ExitWriteLock();
                }
            }

            // Generate model.
            var proxyModel = proxyGeneratorCore.CreateClassProxy(
                type,
                configuration.AdditionalInterfaces,
                ProxyGenerationOptions.Default,
                constructorArguments,
                configuration.InterceptorInstancerSelector(dbContext));

            // Add to proxy type dictionary.
            var addToproxyTypeDictionary = false;
            proxyTypeDictionaryLock.EnterReadLock();
            try
            {
                addToproxyTypeDictionary = !proxyTypeDictionary.ContainsKey(type);
            }
            finally
            {
                proxyTypeDictionaryLock.ExitReadLock();
            }

            if (addToproxyTypeDictionary)
            {
                proxyTypeDictionaryLock.EnterWriteLock();
                try
                {
                    if (!proxyTypeDictionary.ContainsKey(type))
                    {
                        proxyTypeDictionary.Add(type, proxyModel.GetType());
                    }
                }
                finally
                {
                    proxyTypeDictionaryLock.ExitWriteLock();
                }
            }

            return proxyModel;
        }

        public TModel CreateInstance<TModel>(IDbContext dbContext, params object[] constructorArguments) =>
            (TModel)CreateInstance(dbContext, typeof(TModel), constructorArguments);

        public bool IsProxyType(Type type)
        {
            proxyTypeDictionaryLock.EnterReadLock();
            try
            {
                return proxyTypeDictionary.ContainsValue(type);
            }
            finally
            {
                proxyTypeDictionaryLock.ExitReadLock();
            }
        }

        // Protected virtual methods.
        protected virtual IEnumerable<Type> GetCustomAdditionalInterfaces(Type modelType) =>
            Array.Empty<Type>();

        protected virtual IEnumerable<Func<IDbContext, IInterceptor>> GetCustomInterceptorInstancer(Type modelType, IEnumerable<Type> additionalInterfaces) =>
            Array.Empty<Func<IDbContext, IInterceptor>>();

        // Helpers.
        private Type[] GetAdditionalInterfaces(Type modelType)
        {
            var interfaces = new List<Type>();

            // Add custom additional interfaces.
            interfaces.AddRange(GetCustomAdditionalInterfaces(modelType));

            // Add internal additional interfaces.
            if (modelType.GetInterfaces().Contains(typeof(IEntityModel))) //only if is IEntityModel.
            {
                interfaces.Add(typeof(IAuditable));
                interfaces.Add(typeof(IReferenceable));
            }

            return interfaces.ToArray();
        }

        private Func<IDbContext, IInterceptor[]> GetInterceptorInstancer(
            Type modelType,
            IEnumerable<Type> additionalInterfaces)
        {
            var interceptorInstancers = new List<Func<IDbContext, IInterceptor>>();

            // Add custom interceptor instancers.
            interceptorInstancers.AddRange(GetCustomInterceptorInstancer(modelType, additionalInterfaces));

            // Add internal interceptor instances.
            if (modelType.GetInterfaces().Contains(typeof(IEntityModel))) //only if is IEntityModel.
            {
                var entityModelType = modelType.GetInterfaces().First(
                    i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityModel<>));
                var entityModelKeyType = entityModelType.GetGenericArguments().Single();

                //auditableInterceptor
                interceptorInstancers.Add(dbContext => (IInterceptor)Activator.CreateInstance(
                    typeof(AuditableInterceptor<>).MakeGenericType(modelType),
                    additionalInterfaces));

                //summarizableInterceptor
                interceptorInstancers.Add(dbContext => (IInterceptor)Activator.CreateInstance(
                    typeof(ReferenceableInterceptor<,>).MakeGenericType(modelType, entityModelKeyType),
                    additionalInterfaces,
                    dbContext));
            }

            return dbContext => (from instancer in interceptorInstancers
                                 select instancer(dbContext)).ToArray();
        }
    }
}
