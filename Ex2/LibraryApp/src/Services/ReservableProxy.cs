using System;
using LibraryApp.Domain;

namespace LibraryApp.Services
{
    public class ReservableProxy : IReservable
    {
        private readonly LibraryService _service;
        private readonly int _itemId;

        public ReservableProxy(LibraryService service, int itemId)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _itemId = itemId;
        }

        public void Reserve(string userEmail, DateTime from, DateTime to)
            => _service.CreateReservation(_itemId, userEmail, from, to);

        public void CancelReservation(string userEmail)
        {
            var res = _service.GetUserReservations(userEmail);
            foreach (var r in res)
            {
                if (r.Item.Id == _itemId && r.IsActive)
                {
                    _service.CancelReservation(r.Id);
                    return;
                }
            }
            throw new ArgumentException("Active reservation not found for this user and item.");
        }

        public bool IsAvailable()
        {
            var item = _service.GetItem(_itemId) ?? throw new ArgumentException("Item not found.");
            return item.IsAvailable;
        }
    }
}