//   Copyright 2020-present Etherna Sagl
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

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
