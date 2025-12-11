using System.ComponentModel.DataAnnotations;

namespace TransportApi.Api.Models;

public abstract class Vehicle : IReservable
{
    public int Id { get; set; }

    [Required, MaxLength(32)]
    public string RegistrationNumber { get; set; } = string.Empty;

    public double MaxLoadKg { get; set; }

    /// <summary>
    /// Czy pojazd dostępny do nowego zlecenia.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    // IReservable
    public virtual void AssignDriver(Driver driver)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Vehicle is not available.");
        if (!driver.IsAvailable)
            throw new InvalidOperationException("Driver is not available.");
    }

    public virtual void StartOrder()
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Vehicle already in use.");
        IsAvailable = false;
    }

    public virtual void CompleteOrder()
    {
        IsAvailable = true;
    }

    public abstract string GetInfo();
}