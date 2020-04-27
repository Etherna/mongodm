using Digicando.ExecContext;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Digicando.MongODM.AspNetCore
{
    public class HttpContextExecutionContext : IExecutionContext
    {
        // Fields.
        private readonly IHttpContextAccessor httpContextAccessor;

        // Constructors.
        public HttpContextExecutionContext(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        // Properties.
        public IDictionary<object, object>? Items => httpContextAccessor?.HttpContext?.Items;
    }
}
