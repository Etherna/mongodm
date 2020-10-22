using Etherna.MongODM.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Etherna.MongODM.AspNetCore.UI.Areas.MongODM.Pages
{
    public class IndexModel : PageModel
    {
        // Fields.
        private readonly IMongODMConfiguration configuration;
        private readonly IServiceProvider serviceProvider;

        // Constructor.
        public IndexModel(
            IMongODMConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        // Properties.
        public IEnumerable<IDbContext> DbContexts { get; private set; } = default!;

        // Methods.
        public void OnGet()
        {
            InitializePage();
        }

        public async Task OnPostAsync(string identifier)
        {
            InitializePage();

            // Find dbcontext.
            var dbcontext = DbContexts.First(dbc => dbc.Identifier == identifier);

            // Migrate.
            await dbcontext.DbMigrationManager.StartDbContextMigrationAsync().ConfigureAwait(false);
        }

        // Helpers.
        private void InitializePage()
        {
            // Get dbcontext instances.
            var dbContextTypes = configuration.DbContextTypes;
            DbContexts = dbContextTypes.Select(type => (IDbContext)serviceProvider.GetService(type));
        }
    }
}
