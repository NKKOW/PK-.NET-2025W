using System.Collections.Generic;
using System.Linq;
using LibraryApp.Domain;

namespace LibraryApp.Extensions
{
    public static class LibraryExtensions
    {
        public static IEnumerable<T> Available<T>(this IEnumerable<T> items)
            where T : LibraryItem
            => items.Where(i => i.IsAvailable);

        public static IEnumerable<LibraryItem> Newest(this IEnumerable<LibraryItem> items, int take)
            => items.OrderByDescending(i => i.Id).Take(take);
    }
}
