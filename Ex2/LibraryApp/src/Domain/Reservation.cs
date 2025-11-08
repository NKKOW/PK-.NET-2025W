using System;

namespace LibraryApp.Domain
{
    public class Reservation
    {
        public int Id { get; }
        public LibraryItem Item { get; }
        public string UserEmail { get; }
        public DateTime From { get; }
        public DateTime To { get; }
        public bool IsActive { get; private set; } = true;

        public Reservation(int id, LibraryItem item, string userEmail, DateTime from, DateTime to)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            UserEmail = string.IsNullOrWhiteSpace(userEmail) ? throw new ArgumentException("Wymagany adres e-mail.", nameof(userEmail)) : userEmail;
            if (from >= to) throw new ArgumentException("Data początkowa musi być wcześniejsza niż data końcowa.");
            Id = id; From = from; To = to;
        }

        public void Cancel() => IsActive = false;

        public bool ConflictsWith(DateTime from, DateTime to)
            => IsActive && From < to && from < To; 

        public override string ToString()
            => $"Rezerwacja #{Id}: {Item.Title} dla {UserEmail} ({From:yyyy-MM-dd} → {To:yyyy-MM-dd}) {(IsActive ? "[Aktywna]" : "[Anulowana]")}";
    }
}
