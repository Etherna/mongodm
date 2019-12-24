using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Digicando.MongoDM.Utility
{
    class ContextAccessorFacade : IContextAccessorFacade
    {
        // Fields.
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILocalContextAccessor localContextAccessor;
        private readonly IHangfireContextAccessor performContextAccessor;

        // Constructors.
        public ContextAccessorFacade(
            IHttpContextAccessor httpContextAccessor,
            ILocalContextAccessor localContextAccessor,
            IHangfireContextAccessor performContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor; // Provided by Asp.Net, available only on client call.
            this.localContextAccessor = localContextAccessor; // Optional context created by user on local async method stack.
            this.performContextAccessor = performContextAccessor; // Provided by HangFire, available only during task execution.
        }

        // Proeprties.
        public IReadOnlyDictionary<object, object> Items
        {
            get
            {
                if (localContextAccessor.Context != null)
                    return localContextAccessor.Context.Items.ToDictionary(pair => pair.Key as object, pair => pair.Value);
                if (httpContextAccessor.HttpContext != null)
                    return httpContextAccessor.HttpContext.Items.ToDictionary(pair => pair.Key, pair => pair.Value);
                if (performContextAccessor.PerformContext != null)
                    return performContextAccessor.PerformContext.Items.ToDictionary(pair => pair.Key as object, pair => pair.Value);
                throw new InvalidOperationException();
            }
        }

        public object SyncRoot
        {
            get
            {
                if (localContextAccessor.Context != null)
                    return localContextAccessor.Context.Items;
                if (httpContextAccessor.HttpContext != null)
                    return httpContextAccessor.HttpContext.Items;
                if (performContextAccessor.PerformContext != null)
                    return performContextAccessor.PerformContext.Items;
                throw new InvalidOperationException();
            }
        }

        // Methods.
        public void AddItem(string key, object value)
        {
            if (localContextAccessor.Context != null)
                localContextAccessor.Context.Items.Add(key, value);
            else if (httpContextAccessor.HttpContext != null)
                httpContextAccessor.HttpContext.Items.Add(key, value);
            else if (performContextAccessor.PerformContext != null)
                performContextAccessor.PerformContext.Items.Add(key, value);
            else
                throw new InvalidOperationException();
        }

        public bool RemoveItem(string key)
        {
            if (localContextAccessor.Context != null)
                return localContextAccessor.Context.Items.Remove(key);
            else if (httpContextAccessor.HttpContext != null)
                return httpContextAccessor.HttpContext.Items.Remove(key);
            else if (performContextAccessor.PerformContext != null)
                return performContextAccessor.PerformContext.Items.Remove(key);
            else
                throw new InvalidOperationException();
        }
    }
}
