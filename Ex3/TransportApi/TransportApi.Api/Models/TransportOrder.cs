using System.ComponentModel.DataAnnotations;

namespace TransportApi.Api.Models;

public class TransportOrder
{
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string CargoDescription { get; set; } = string.Empty;

    public double Weight { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; } = false;

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int DriverId { get; set; }
    public Driver? Driver { get; set; }
}