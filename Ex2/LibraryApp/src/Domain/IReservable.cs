using System;

namespace LibraryApp.Domain
{
    public interface IReservable
    {
        void Reserve(string userEmail, DateTime from, DateTime to);
        void CancelReservation(string userEmail);
        bool IsAvailable();
    }
}