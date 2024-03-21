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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Etherna.MongODM.Core.Serialization.Mapping
{
    public static class MemberMapRenderHelper
    {
        public static string RenderElementPath(
            IEnumerable<IMemberMap> memberMapsPath,
            bool referToFinalItem,
            Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector,
            Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector)
        {
            var sb = new StringBuilder();

            foreach (var memberMap in memberMapsPath)
            {
                if (sb.Length != 0)
                    sb.Append('.');

                sb.Append(memberMap.BsonMemberMap.ElementName);

                //don't render final item element path, if not required
                if (referToFinalItem || memberMap != memberMapsPath.Last())
                    sb.Append(memberMap.RenderInternalItemElementPath(
                        undefinedArrayIndexSymbolSelector,
                        undefinedDocumentElementSymbolSelector));
            }

            return sb.ToString();
        }

        public static string RenderInternalItemElementPath(
            IEnumerable<ElementRepresentationBase> elementsPath,
            Func<ArrayElementRepresentation, string> undefinedArrayIndexSymbolSelector,
            Func<DocumentElementRepresentation, string> undefinedDocumentElementSymbolSelector)
        {
            var sb = new StringBuilder();

            foreach (var element in elementsPath)
            {
                switch (element)
                {
                    case ArrayElementRepresentation arrayElementPathRepresentation:
                        sb.Append(arrayElementPathRepresentation.ItemIndex is null ?
                            undefinedArrayIndexSymbolSelector(arrayElementPathRepresentation) :
                            $".{arrayElementPathRepresentation.ItemIndex}");
                        break;
                    case DocumentElementRepresentation documentElementPathRepresentation:
                        sb.Append(documentElementPathRepresentation.ElementName is null ?
                            undefinedDocumentElementSymbolSelector(documentElementPathRepresentation) :
                            $".{documentElementPathRepresentation.ElementName}");
                        break;
                    default: throw new NotSupportedException();
                }
            }

            return sb.ToString();
        }
    }
}
