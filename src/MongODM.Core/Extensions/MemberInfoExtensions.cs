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

using System.Linq;
using System.Reflection;

namespace Etherna.MongODM.Core.Extensions
{
    public static class MemberInfoExtensions
    {
        public static bool IsSameAs(this MemberInfo memberInfo, MemberInfo otherMemberInfo)
        {
            if (memberInfo == null)
                return otherMemberInfo == null;
            if (otherMemberInfo == null)
                return false;
            if (Equals(memberInfo, otherMemberInfo))
                return true;
            if (memberInfo.Name != otherMemberInfo.Name)
                return false;
            
            var memberInfoDeclaringType = memberInfo.DeclaringType!;
            var otherMemberInfoDeclaringType = otherMemberInfo.DeclaringType!;
            return memberInfoDeclaringType == otherMemberInfoDeclaringType ||
                   memberInfoDeclaringType.GetTypeInfo().IsSubclassOf(otherMemberInfoDeclaringType) ||
                   otherMemberInfoDeclaringType.GetTypeInfo().IsSubclassOf(memberInfoDeclaringType) ||
                   memberInfoDeclaringType.GetTypeInfo().ImplementedInterfaces.Contains(otherMemberInfo.DeclaringType) ||
                   otherMemberInfoDeclaringType.GetTypeInfo().ImplementedInterfaces.Contains(memberInfo.DeclaringType);
        }
    }
}
