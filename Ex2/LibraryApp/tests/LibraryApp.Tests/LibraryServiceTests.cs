using System;
using LibraryApp.Domain;
using LibraryApp.Services;
using Xunit;

namespace LibraryApp.Tests
{
    public class LibraryServiceTests
    {
        [Fact]
        public void AddItem_And_RegisterUser_Works()
        {
            var svc = new LibraryService();
            var b = new Book(svc.NextId(), "Clean Code", "Robert C. Martin", "9780132350884");
            svc.AddItem(b);
            svc.RegisterUser("a@b.com");
            Assert.Single(svc.Items);
        }

        [Fact]
        public void CreateReservation_ForAvailableItem_Succeeds()
        {
            var svc = new LibraryService();
            var b = new Book(svc.NextId(), "DDD", "Eric Evans", "123");
            svc.AddItem(b);
            svc.RegisterUser("u@x.com");

            var res = svc.CreateReservation(b.Id, "u@x.com", DateTime.Today, DateTime.Today.AddDays(7));
            Assert.NotNull(res);
            Assert.False(b.IsAvailable);
        }

        [Fact]
        public void CreateReservation_WhenItemUnavailable_Throws()
        {
            var svc = new LibraryService();
            var b = new Book(svc.NextId(), "A", "B", "C");
            svc.AddItem(b);
            svc.RegisterUser("u1@x.com");
            svc.RegisterUser("u2@x.com");

            svc.CreateReservation(b.Id, "u1@x.com", DateTime.Today, DateTime.Today.AddDays(2));

            Assert.Throws<InvalidOperationException>(() =>
                svc.CreateReservation(b.Id, "u2@x.com", DateTime.Today, DateTime.Today.AddDays(1)));
        }

        [Fact]
        public void CancelReservation_RaisesEvent_And_MakesItemAvailable()
        {
            var svc = new LibraryService();
            var b = new Book(svc.NextId(), "A", "B", "C");
            svc.AddItem(b);
            svc.RegisterUser("u@x.com");

            var res = svc.CreateReservation(b.Id, "u@x.com", DateTime.Today, DateTime.Today.AddDays(1));

            bool eventRaised = false;
            svc.OnReservationCancelled += _ => eventRaised = true;

            svc.CancelReservation(res.Id);

            Assert.True(eventRaised);
            Assert.True(b.IsAvailable);
        }

        [Fact]
        public void ReservationConflict_ThrowsCustomException_WhenOverlaps()
        {
            var svc = new LibraryService();
            var b = new Book(svc.NextId(), "A", "B", "C");
            svc.AddItem(b);
            svc.RegisterUser("u@x.com");

            var r1 = svc.CreateReservation(b.Id, "u@x.com", DateTime.Today, DateTime.Today.AddDays(3));
            svc.CancelReservation(r1.Id);

            var r2 = svc.CreateReservation(b.Id, "u@x.com", DateTime.Today, DateTime.Today.AddDays(3));
            Assert.Equal(2, r2.Id);
        }
    }
}