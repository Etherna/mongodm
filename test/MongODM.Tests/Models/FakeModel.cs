using System.Collections.Generic;

namespace Digicando.MongODM.Models
{
    public class FakeModel : FakeEntityModelBase<string>
    {
        public virtual IEnumerable<FakeModel> EnumerableProp { get; set; }
        public virtual int IntegerProp { get; set; }
        public virtual FakeModel ObjectProp { get; set; }
        public virtual string StringProp { get; set; }
    }
}
