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

namespace Etherna.MongODM.Core.Utility
{
    /// <summary>
    /// This interface has the scope to inizialize only one time static configurations, when IoC system
    /// has been configured, dependencies can be resolved, and before that any dbcontext starts to operate.
    /// For a proper use, implements it in a class where configuration is invoked by constructor.
    /// So configure it as a singleton on IoC system, and injectit as a dependency for DbContext.
    /// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
    public interface IStaticConfigurationBuilder
    {
    }
#pragma warning restore CA1040 // Avoid empty interfaces
}