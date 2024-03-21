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
