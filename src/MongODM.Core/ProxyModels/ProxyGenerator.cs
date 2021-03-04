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

using Castle.DynamicProxy;
using Etherna.MongODM.Core.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Etherna.MongODM.Core.ProxyModels
{
    public class ProxyGenerator : IProxyGenerator, IDisposable
    {
        // Fields.
        private readonly Castle.DynamicProxy.IProxyGenerator proxyGeneratorCore;

        private readonly Dictionary<Type,
            (Type[] AdditionalInterfaces, Func<IDbContext, IInterceptor[]> InterceptorInstancerSelector)> modelConfigurationDictionary = new();
        private readonly ReaderWriterLockSlim modelConfigurationDictionaryLock = new(LockRecursionPolicy.SupportsRecursion);

        private readonly Dictionary<Type, Type> proxyTypeDictionary = new();
        private readonly ReaderWriterLockSlim proxyTypeDictionaryLock = new(LockRecursionPolicy.SupportsRecursion);

        // Constructors.
        public ProxyGenerator(
            Castle.DynamicProxy.IProxyGenerator proxyGeneratorCore)
        {
            this.proxyGeneratorCore = proxyGeneratorCore;
        }

        // Methods.
        public object CreateInstance(
            Type type,
            IDbContext dbContext,
            params object[] constructorArguments)
        {
            if (dbContext is null)
                throw new ArgumentNullException(nameof(dbContext));
            if (type is null)
                throw new ArgumentNullException(nameof(type));

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
            (TModel)CreateInstance(typeof(TModel), dbContext, constructorArguments);

        public void Dispose()
        {
            modelConfigurationDictionaryLock.Dispose();
            proxyTypeDictionaryLock.Dispose();
            GC.SuppressFinalize(this);
        }

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

        public Type PurgeProxyType(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            return IsProxyType(type) ?
                type.BaseType :
                type;
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
