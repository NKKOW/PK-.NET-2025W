using Microsoft.EntityFrameworkCore;
using TransportApi.Api.Data;
using TransportApi.Api.Models;

namespace TransportApi.Api.Services;

public class FleetManager
{
    private readonly FleetDbContext _db;

    public FleetManager(FleetDbContext db) => _db = db;

    public event Action<string>? OnNewOrderCreated;

    public async Task<TransportOrder> CreateOrderAsync(string cargoDescription, double weight, int vehicleId, int driverId, CancellationToken ct = default)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId, ct)
            ?? throw new InvalidOperationException("Vehicle not found.");

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct)
            ?? throw new InvalidOperationException("Driver not found.");

        if (!vehicle.IsAvailable) throw new InvalidOperationException("Vehicle not available.");
        if (!driver.IsAvailable) throw new InvalidOperationException("Driver not available.");
        if (weight > vehicle.MaxLoadKg) throw new InvalidOperationException("Weight exceeds vehicle capacity.");
        
        vehicle.AssignDriver(driver);
        vehicle.StartOrder();

        driver.IsAvailable = false;

        var order = new TransportOrder
        {
            CargoDescription = cargoDescription,
            Weight = weight,
            VehicleId = vehicle.Id,
            DriverId = driver.Id,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        OnNewOrderCreated?.Invoke($"New order #{order.Id} for {cargoDescription} created at {order.CreatedAt:u}");

        return order;
    }

    public async Task CompleteOrderAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Vehicle)
            .Include(o => o.Driver)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.IsCompleted) return;

        order.IsCompleted = true;

        if (order.Vehicle is not null)
            order.Vehicle.CompleteOrder();

        if (order.Driver is not null)
            order.Driver.IsAvailable = true;

        await _db.SaveChangesAsync(ct);
    }
}
