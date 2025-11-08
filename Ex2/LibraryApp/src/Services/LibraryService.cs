using System;
using System.Collections.Generic;
using System.Linq;
using LibraryApp.Domain;

namespace LibraryApp.Services
{
    public class LibraryService
    {
        private readonly List<LibraryItem> _items = new();
        private readonly List<Reservation> _reservations = new();
        private readonly HashSet<string> _users = new(StringComparer.OrdinalIgnoreCase);

        private int _nextItemId = 1;
        private int _nextReservationId = 1;

        public event Action<Reservation>? OnNewReservation;
        public event Action<Reservation>? OnReservationCancelled;

        public int NextId() => _nextItemId++;

        public void AddItem(LibraryItem item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            if (_items.Any(i => i.Id == item.Id))
                throw new ArgumentException($"Pozycja o identyfikatorze {item.Id} już istnieje.");
            _items.Add(item);
        }

        public void RegisterUser(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Wymagany adres e-mail.", nameof(email));
            if (!_users.Add(email)) throw new ArgumentException("Użytkownik jest już zarejestrowany.", nameof(email));
        }

        public IEnumerable<LibraryItem> ListAvailableItems(Func<LibraryItem, bool>? predicate = null)
        {
            var q = _items.Where(i => i.IsAvailable);
            if (predicate != null) q = q.Where(predicate);
            return q.ToList();
        }

        public LibraryItem? GetItem(int itemId) => _items.FirstOrDefault(i => i.Id == itemId);

        public IEnumerable<Reservation> GetUserReservations(string userEmail)
        {
            if (string.IsNullOrWhiteSpace(userEmail)) throw new ArgumentException("Wymagany adres e-mail.", nameof(userEmail));
            return _reservations.Where(r => r.UserEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public Reservation CreateReservation(int itemId, string userEmail, DateTime from, DateTime to)
        {
            if (!_users.Contains(userEmail)) throw new ArgumentException("Nieznany użytkownik.", nameof(userEmail));
            var item = GetItem(itemId) ?? throw new ArgumentException("Nie znaleziono pozycji.", nameof(itemId));
            if (!item.IsAvailable) throw new InvalidOperationException("Pozycja jest niedostępna.");
            if (from >= to) throw new ArgumentException("Data początkowa musi być wcześniejsza niż data końcowa.");
            if (_reservations.Any(r => r.Item.Id == itemId && r.ConflictsWith(from, to)))
                throw new ReservationConflictException("Rezerwacja koliduje z istniejącą aktywną rezerwacją.");

            var reservation = new Reservation(_nextReservationId++, item, userEmail, from, to);
            _reservations.Add(reservation);
            
            item.IsAvailable = false;

            OnNewReservation?.Invoke(reservation);
            return reservation;
        }

        public void CancelReservation(int reservationId)
        {
            var res = _reservations.FirstOrDefault(r => r.Id == reservationId)
                      ?? throw new ArgumentException("Nie znaleziono rezerwacji.", nameof(reservationId));

            if (!res.IsActive) return;

            res.Cancel();
            
            var item = res.Item;
            var stillActive = _reservations.Any(r => r.Item.Id == item.Id && r.IsActive);
            if (!stillActive) item.IsAvailable = true;

            OnReservationCancelled?.Invoke(res);
        }

        public IEnumerable<LibraryItem> Search(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return _items;
            phrase = phrase.Trim();
            return _items.Where(i =>
                    i.Title.Contains(phrase, StringComparison.OrdinalIgnoreCase) ||
                    (i is Book b && b.Author.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        
        public IReadOnlyList<Reservation> Reservations => _reservations.AsReadOnly();
        public IReadOnlyList<LibraryItem> Items => _items.AsReadOnly();
    }
}
