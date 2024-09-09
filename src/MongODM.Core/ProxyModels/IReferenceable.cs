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

using System.Collections.Generic;

namespace Etherna.MongODM.Core.ProxyModels
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