using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCore.UI.Areas.MongODM.Pages
{
    public class IndexModel : PageModel
    {
        // Fields.
        private readonly ISsoDbContext ssoDbContext;
        private readonly UserManager<User> userManager;

        // Constructor.
        public MigrationModel(
            ISsoDbContext ssoDbContext,
            UserManager<User> userManager)
        {
            this.ssoDbContext = ssoDbContext;
            this.userManager = userManager;
        }

        // Methods.
        public void OnGet()
        {
        }

        public async Task OnPostAsync()
        {
            var userId = userManager.GetUserId(User);
            await ssoDbContext.DbMigrationManager.StartDbContextMigrationAsync(userId);
        }
    }
}
