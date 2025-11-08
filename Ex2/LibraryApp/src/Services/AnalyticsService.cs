using System;
using System.Linq;
using LibraryApp.Domain;

namespace LibraryApp.Services
{
    // LibraryService i liczy metryki na jego kolekcjach

    public class AnalyticsService
    {
        private readonly LibraryService _service;

        public AnalyticsService(LibraryService service) => _service = service ?? throw new ArgumentNullException(nameof(service));

        public double AverageLoanLengthDays()
        {
            var list = _service.Reservations.ToList();
            if (list.Count == 0) return 0.0;
            return list.Average(r => (r.To - r.From).TotalDays);
        }

        public int TotalLoans() => _service.Reservations.Count;

        public string MostPopularItemTitle()
        {
            if (_service.Reservations.Count == 0) return "(brak danych)";
            return _service.Reservations
                .GroupBy(r => r.Item.Title)
                .Select(g => new { Title = g.Key, Count = g.Count(), MaxId = g.Max(x => x.Item.Id) })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.MaxId)
                .First().Title;
        }

        public double FulfillmentRate()
        {
            if (_service.Reservations.Count == 0) return 0.0;
            var notCancelled = _service.Reservations.Count(r => r.IsActive);
            return (double)notCancelled / _service.Reservations.Count;
        }

        public double LogPopularityScore(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Wymagany tytuł.", nameof(title));
            var n = _service.Reservations.Count(r => r.Item.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(title), "Popularność musi być dodatnia, aby obliczyć logarytm.");
            return Math.Log(n);
        }
    }
}
