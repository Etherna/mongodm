﻿using System;

namespace Digicando.MongODM.ProxyModels
{
    public interface IProxyGenerator
    {
        object CreateInstance(IDbContext dbContext, Type type, params object[] constructorArguments);
        TModel CreateInstance<TModel>(IDbContext dbContext, params object[] constructorArguments);
        bool IsProxyType(Type type);
    }
}