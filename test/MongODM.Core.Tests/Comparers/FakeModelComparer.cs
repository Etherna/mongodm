using Etherna.MongODM.Models;
using System.Collections.Generic;
using System.Linq;

namespace Etherna.MongODM.Comparers
{
    public class FakeModelComparer : EqualityComparer<FakeModel?>
    {
        public override bool Equals(FakeModel? x, FakeModel? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            if ((x.EnumerableProp == null || y.EnumerableProp == null) &&
                !(x.EnumerableProp == null && y.EnumerableProp == null))
                return false;
            if (x.EnumerableProp != null)
            {
                if (x.EnumerableProp.Count() != y.EnumerableProp.Count())
                    return false;
                if (x.EnumerableProp.Zip(y.EnumerableProp, (xVal, yVal) => new { xVal, yVal })
                    .Any(pair => !Equals(pair.xVal, pair.yVal)))
                    return false;
            }

            if ((x.ExtraElements == null || y.ExtraElements == null) &&
                !(x.ExtraElements == null && y.ExtraElements == null))
                return false;
            if (x.ExtraElements != null)
            {
                if (x.ExtraElements.Count() != y.ExtraElements.Count())
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
