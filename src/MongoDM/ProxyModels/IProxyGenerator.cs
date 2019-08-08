using System;

namespace Digicando.MongoDM.ProxyModels
{
    public interface IProxyGenerator
    {
        object CreateInstance(Type type, params object[] constructorArguments);
        TModel CreateInstance<TModel>(params object[] constructorArguments);
        bool IsProxyType(Type type);
    }
}