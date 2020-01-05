using System.Collections.Generic;
using System.Reflection;

namespace Digicando.MongODM.ProxyModels
{
    public interface IAuditable
    {
        // Properties.
        IEnumerable<MemberInfo> ChangedMembers { get; }
        bool IsAuditingEnabled { get; }
        bool IsChanged { get; }

        // Methods.
        void DisableAuditing();
        void EnableAuditing();
        void ResetChangedMembers();
    }
}
