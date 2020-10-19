using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCore.UI.Auth.Filters
{
    public interface IDashboardAuthFilter
    {
        Task<bool> AuthorizeAsync(HttpContext context);
    }
}