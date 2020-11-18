using System.Linq;
using System.Reflection;

namespace Etherna.MongODM.Core.Extensions
{
    public static class MemberInfoExtensions
    {
        public static bool IsSameAs(this MemberInfo memberInfo, MemberInfo otherMemberInfo)
            => memberInfo == null
                ? otherMemberInfo == null
                : (otherMemberInfo != null &&
                    (Equals(memberInfo, otherMemberInfo)
                        || (memberInfo.Name == otherMemberInfo.Name
                            && (memberInfo.DeclaringType == otherMemberInfo.DeclaringType
                                || memberInfo.DeclaringType.GetTypeInfo().IsSubclassOf(otherMemberInfo.DeclaringType)
                                || otherMemberInfo.DeclaringType.GetTypeInfo().IsSubclassOf(memberInfo.DeclaringType)
                                || memberInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces.Contains(otherMemberInfo.DeclaringType)
                                || otherMemberInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces
                                    .Contains(memberInfo.DeclaringType)))));
    }
}
