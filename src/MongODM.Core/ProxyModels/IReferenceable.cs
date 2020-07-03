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