using System;

namespace Digicando.MongoDM.Utility
{
    public interface ILocalContextFactory
    {
        IDisposable CreateNewLocalContext();
    }
}