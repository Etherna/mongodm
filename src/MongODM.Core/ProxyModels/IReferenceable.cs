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

using System.Collections.Generic;

namespace Etherna.MongODM.ProxyModels
{
    public interface IReferenceable
	{
        // Properties.
        /// <summary>
        /// True if current model is a summary refered model
        /// </summary>
		bool IsSummary { get; }

        /// <summary>
        /// Name list of current setted members
        /// </summary>
        IEnumerable<string> SettedMemberNames { get; }

        // Methods.
        /// <summary>
        /// Clear the setted members list
        /// </summary>
        void ClearSettedMembers();

        /// <summary>
        /// Merge current summary model with a full model
        /// </summary>
        /// <param name="fullModel">The full model</param>
        void MergeFullModel(object fullModel);

        /// <summary>
        /// Set a list of member names as coming from summary loading
        /// </summary>
        /// <param name="summaryLoadedMemberNames">The member name list</param>
        void SetAsSummary(IEnumerable<string> summaryLoadedMemberNames);
    }
}