using System.Collections.Generic;

namespace Etherna.MongODM.Core.Repositories
{
    public class PaginatedEnumerable<TClass>
    {
        public PaginatedEnumerable(
            IEnumerable<TClass> elements,
            int currentPage,
            int pageSize,
            int maxPage)
        {
            CurrentPage = currentPage;
            Elements = elements;
            MaxPage = maxPage;
            PageSize = pageSize;
        }

        public int CurrentPage { get; }
        public IEnumerable<TClass> Elements { get; }
        public int MaxPage { get; }
        public int PageSize { get; }
    }
}
