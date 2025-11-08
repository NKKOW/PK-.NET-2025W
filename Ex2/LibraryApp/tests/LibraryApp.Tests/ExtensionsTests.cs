using System.Linq;
using LibraryApp.Domain;
using LibraryApp.Extensions;
using LibraryApp.Services;
using Xunit;

namespace LibraryApp.Tests
{
    public class ExtensionsTests
    {
        [Fact]
        public void Available_FiltersByAvailability()
        {
            var svc = new LibraryService();
            var b1 = new Book(svc.NextId(), "A", "B", "C");
            var b2 = new Book(svc.NextId(), "D", "E", "F");
            svc.AddItem(b1); svc.AddItem(b2);

            var available = svc.Items.OfType<Book>().Available().ToList();
            Assert.Equal(2, available.Count);
        }

        [Fact]
        public void Newest_TakesByIdDescending()
        {
            var svc = new LibraryService();
            var b1 = new Book(svc.NextId(), "A", "B", "C"); // id 1
            var b2 = new Book(svc.NextId(), "D", "E", "F"); // id 2
            svc.AddItem(b1); svc.AddItem(b2);

            var newest = svc.Items.Newest(1).Single();
            Assert.Equal(2, newest.Id);
        }
    }
}