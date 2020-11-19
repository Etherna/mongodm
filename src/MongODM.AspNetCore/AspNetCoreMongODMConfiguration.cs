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

using Etherna.MongODM.Core;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Etherna.MongODM.AspNetCore
{
    public class AspNetCoreMongODMConfiguration : MongODMConfiguration
    {
        // Fields.
        private readonly IServiceCollection services;

        // Constructor.
        public AspNetCoreMongODMConfiguration(IServiceCollection services)
        {
            this.services = services;
        }

        // Protected methods.
        protected override void RegisterSingleton<TService>() =>
            services.AddSingleton<TService>();

        protected override void RegisterSingleton<TService>(TService instance) =>
            services.AddSingleton(instance);

        protected override void RegisterSingleton<TService, TImplementation>() =>
            services.AddSingleton<TService, TImplementation>();

        protected override void RegisterSingleton<TService>(Func<IServiceProvider, TService> implementationFactory) =>
            services.AddSingleton(implementationFactory);
    }
}
