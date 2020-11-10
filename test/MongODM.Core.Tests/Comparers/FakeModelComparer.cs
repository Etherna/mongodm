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

using Etherna.MongODM.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Core.Comparers
{
    public class FakeModelComparer : EqualityComparer<FakeModel?>
    {
        public override bool Equals(FakeModel? x, FakeModel? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            //(x.EnumerableProp is null) XOR (y.EnumerableProp is null)
            if ((x.EnumerableProp == null || y.EnumerableProp == null) &&
                !(x.EnumerableProp == null && y.EnumerableProp == null))
                return false;
            //because prev XOR is false, if (x.EnumerableProp != null) so (y.EnumerableProp != null)
            if (x.EnumerableProp != null && y.EnumerableProp != null)
            {
                if (x.EnumerableProp.Count() != y.EnumerableProp.Count())
                    return false;
                if (x.EnumerableProp.Zip(y.EnumerableProp, (xVal, yVal) => new { xVal, yVal })
                    .Any(pair => !Equals(pair.xVal, pair.yVal)))
                    return false;
            }

            //XOR
            if ((x.ExtraElements == null || y.ExtraElements == null) &&
                !(x.ExtraElements == null && y.ExtraElements == null))
                return false;
            //as before
            if (x.ExtraElements != null && y.ExtraElements != null)
            {
                if (x.ExtraElements.Count != y.ExtraElements.Count)
                    return false;
                if (x.ExtraElements.Zip(y.ExtraElements, (xVal, yVal) => new { xVal, yVal })
                    .Any(pair => !Equals(pair.xVal, pair.yVal)))
                    return false;
            }

            if (x.Id != y.Id)
                return false;

            if (x.IntegerProp != y.IntegerProp)
                return false;

            if (!Equals(x.ObjectProp, y.ObjectProp))
                return false;

            if (x.StringProp != y.StringProp)
                return false;

            return true;
        }

        public override int GetHashCode(FakeModel? obj)
        {
            if (obj is null) return -1;
            return obj.GetHashCode();
        }
    }
}
