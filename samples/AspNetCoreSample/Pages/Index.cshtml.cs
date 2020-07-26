using Etherna.MongODM.AspNetCoreSample.Models;
using Etherna.MongODM.AspNetCoreSample.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace AspNetCoreSample.Pages
{
    public class IndexModel : PageModel
    {
        // Models.
        public class InputModel
        {
            [Required]
            [DataType(DataType.Date)]
            public DateTime Birthday { get; set; }

            [Required]
            public string Name { get; set; }
        }

        // Fields.
        private readonly ISampleDbContext sampleDbContext;

        // Constructor.
        public IndexModel(ISampleDbContext sampleDbContext)
        {
            this.sampleDbContext = sampleDbContext;
        }

        // Properties.
        public List<Cat> Cats { get; } = new List<Cat>();

        [BindProperty]
        public InputModel Input { get; set; }

        // Methods.
        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCatsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCatsAsync();

            if (!ModelState.IsValid)
                return Page();

            var cat = new Cat(Input.Name, Input.Birthday);
            await sampleDbContext.Cats.CreateAsync(cat);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(string id)
        {
            await sampleDbContext.Cats.DeleteAsync(id);

            return RedirectToPage();
        }

        // Private helpers.
        private async Task LoadCatsAsync()
        {
            var cats = await sampleDbContext.Cats.QueryElementsAsync(elements =>
                elements.ToListAsync());

            Cats.AddRange(cats);
        }
    }
}
