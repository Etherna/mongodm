using System;
using System.Collections.Generic;

namespace Etherna.MongODM.Core.Options
{
    public interface IMongODMOptionsBuilder
    {
        void SetDbContextTypes(IEnumerable<Type> dbContextTypes);
    }
}
