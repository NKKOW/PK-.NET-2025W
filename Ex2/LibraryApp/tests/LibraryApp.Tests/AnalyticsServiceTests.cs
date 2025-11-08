using System;
using LibraryApp.Domain;
using LibraryApp.Services;
using Xunit;

namespace LibraryApp.Tests
{
    public class AnalyticsServiceTests
    {
        [Fact]
        public void AverageLoanLength_Empty_ReturnsZero()
        {
            var svc = new LibraryService();
            var analytics = new AnalyticsService(svc);
            Assert.Equal(0.0, analytics.AverageLoanLengthDays());
        }

        [Fact]
        public void AverageLoanLength_Computed()
        {
            var svc = new LibraryService();
            var analytics = new AnalyticsService(svc);
            var b = new Book(svc.NextId(), "A", "B", "C");
            svc.AddItem(b);
            svc.RegisterUser("u@x.com");
            svc.CreateReservation(b.Id, "u@x.com", DateTime.Today, DateTime.Today.AddDays(4));
            Assert.Equal(4.0, analytics.AverageLoanLengthDays(), 3);
        }

        [Fact]
        public void MostPopularItem_Works_WithOrWithoutData()
        {
            var svc = new LibraryService();
            var analytics = new AnalyticsService(svc);
            Assert.Equal("(brak danych)", analytics.MostPopularItemTitle());

            var b = new Book(svc.NextId(), "Zed", "A", "1");
            svc.AddItem(b);
            svc.RegisterUser("u@x.com");
            svc.CreateReservation(b.Id, "u@x.com", DateTime.Today, DateTime.Today.AddDays(1));

            Assert.Equal("Zed", analytics.MostPopularItemTitle());
        }

        [Fact]
        public void LogPopularity_PositiveOnly()
        {
            var svc = new LibraryService();
            var analytics = new AnalyticsService(svc);
            var b = new Book(svc.NextId(), "Zed", "A", "1");
            svc.AddItem(b);
            svc.RegisterUser("u@x.com");
            svc.CreateReservation(b.Id, "u@x.com", DateTime.Today, DateTime.Today.AddDays(1));

            var v = analytics.LogPopularityScore("Zed");
            Assert.True(v >= 0); 

            Assert.Throws<ArgumentOutOfRangeException>(() => analytics.LogPopularityScore("Unknown"));
        }
    }
}