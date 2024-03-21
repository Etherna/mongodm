// Copyright 2020-present Etherna SA
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Etherna.MongODM.AspNetCore;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.ProxyModels;
using Etherna.MongODM.HF.Tasks;
using Hangfire;
using Hangfire.Mongo;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Etherna.MongODM
{
    public static class ServiceCollectionExtensions
    {
        // Methods.
        public static IMongODMConfiguration AddMongODMWithHangfire(
            this IServiceCollection services,
            Action<HangfireOptions>? configureHangfireOptions = null,
            Action<MongODMOptions>? configureMongODMOptions = null)
        {
            // Configure MongODM.
            var conf = services.AddMongODM<HangfireTaskRunner>(configureMongODMOptions);

            // Configure Hangfire.
            AddHangfire(services, configureHangfireOptions);

            return conf;
        }

        public static IMongODMConfiguration AddMongODMWithHangfire<TProxyGenerator>(
            this IServiceCollection services,
            Action<HangfireOptions>? configureHangfireOptions = null,
            Action<MongODMOptions>? configureMongODMOptions = null)
            where TProxyGenerator : class, IProxyGenerator
        {
            // Configure MongODM.
            var conf = services.AddMongODM<TProxyGenerator, HangfireTaskRunner>(configureMongODMOptions);

            // Configure Hangfire.
            AddHangfire(services, configureHangfireOptions);

            return conf;
        }

        // Helpers.
        private static void AddHangfire(
            IServiceCollection services,
            Action<HangfireOptions>? configureHangfireOptions)
        {
            // Configure options.
            var hangfireOptions = new HangfireOptions();
            configureHangfireOptions?.Invoke(hangfireOptions);

            // Add hangfire.
            services.AddHangfire(options =>
            {
                options.UseMongODM();
                options.UseMongoStorage(hangfireOptions.ConnectionString, hangfireOptions.StorageOptions);
            });
        }
    }
}
