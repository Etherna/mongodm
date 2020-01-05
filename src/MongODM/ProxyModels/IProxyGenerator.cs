using System;

namespace Digicando.MongODM.ProxyModels
{
    public interface IProxyGenerator
    {
        object CreateInstance(Type type, params object[] constructorArguments);
        TModel CreateInstance<TModel>(params object[] constructorArguments);
        bool IsProxyType(Type type);
    }
}