using System;
using System.Data.Common;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TransportApi.Api.Data;
using TransportApi.Api.Extensions;
using TransportApi.Api.Models;
using TransportApi.Api.Services;
using Xunit;

namespace TransportApi.Tests;

public class FleetTests : IAsyncLifetime
{
    private readonly DbConnection _conn;
    private FleetDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FleetDbContext>()
            .UseSqlite(_conn)
            .Options;
        return new FleetDbContext(options);
    }

    public FleetTests()
    {
        _conn = new SqliteConnection("Filename=:memory:");
        _conn.Open();
    }

    public async Task InitializeAsync()
    {
        using var db = CreateContext();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        _conn.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Add_Vehicle_And_Driver()
    {
        using var db = CreateContext();

        db.Vehicles.Add(new Truck { RegistrationNumber = "WX12345", MaxLoadKg = 24000, TrailerLength = 13.6 });
        db.Drivers.Add(new Driver { Name = "Jan Kowalski", LicenseNumber = "C+E/1234" });
        await db.SaveChangesAsync();

        (await db.Vehicles.CountAsync()).Should().Be(1);
        (await db.Drivers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Create_Order_Sets_Availability_And_Raises_Event()
    {
        using var db = CreateContext();

        var vehicle = new Van { RegistrationNumber = "KR11111", MaxLoadKg = 1500, CargoVolume = 10 };
        var driver  = new Driver { Name = "Anna Nowak", LicenseNumber = "B/5678" };
        db.AddRange(vehicle, driver);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);

        string? eventMessage = null;
        mgr.OnNewOrderCreated += msg => eventMessage = msg;

        var order = await mgr.CreateOrderAsync("Elektronika - ładunek 1.2t", 1200, vehicle.Id, driver.Id);

        order.Id.Should().BeGreaterThan(0);
        (await db.Orders.CountAsync()).Should().Be(1);

        (await db.Vehicles.FindAsync(vehicle.Id))!.IsAvailable.Should().BeFalse();
        (await db.Drivers.FindAsync(driver.Id))!.IsAvailable.Should().BeFalse();

        eventMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Complete_Order_Frees_Resources()
    {
        using var db = CreateContext();

        var truck = new Truck { RegistrationNumber = "PO22222", MaxLoadKg = 18000, TrailerLength = 12 };
        var drv   = new Driver { Name = "Marek Test", LicenseNumber = "C/8888" };
        db.AddRange(truck, drv);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);
        var order = await mgr.CreateOrderAsync("Palety", 5000, truck.Id, drv.Id);

        await mgr.CompleteOrderAsync(order.Id);

        (await db.Orders.FindAsync(order.Id))!.IsCompleted.Should().BeTrue();
        (await db.Vehicles.FindAsync(truck.Id))!.IsAvailable.Should().BeTrue();
        (await db.Drivers.FindAsync(drv.Id))!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableVehicles_Extension_Works()
    {
        using var db = CreateContext();

        db.Vehicles.AddRange(
            new Van { RegistrationNumber = "GD-A1", MaxLoadKg = 1200, CargoVolume = 8, IsAvailable = true },
            new Truck { RegistrationNumber = "GD-A2", MaxLoadKg = 24000, TrailerLength = 13.6, IsAvailable = false },
            new Van { RegistrationNumber = "GD-A3", MaxLoadKg = 800, CargoVolume = 6, IsAvailable = true }
        );
        await db.SaveChangesAsync();

        var available = await db.Vehicles.GetAvailableVehicles().ToListAsync();
        available.Should().HaveCount(2);

        var withCapacity = await db.Vehicles.GetAvailableVehicles(1000).ToListAsync();
        withCapacity.Should().Satisfy(v => v.MaxLoadKg >= 1000);
    }

    [Fact]
    public async Task Create_Order_Rejects_Too_Heavy_Load()
    {
        using var db = CreateContext();

        var van = new Van { RegistrationNumber = "WA-HEAVY", MaxLoadKg = 1000, CargoVolume = 7 };
        var drv = new Driver { Name = "Ola", LicenseNumber = "B/888" };
        db.AddRange(van, drv);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);

        Func<Task> act = () => mgr.CreateOrderAsync("Zbyt ciężkie", 2000, van.Id, drv.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds vehicle capacity*");
    }
    
        [Fact]
    public async Task Create_Order_Rejects_Unavailable_Vehicle()
    {
        using var db = CreateContext();

        var truck = new Truck { RegistrationNumber = "WX-UNAVL", MaxLoadKg = 10000, TrailerLength = 12, IsAvailable = false };
        var drv   = new Driver { Name = "Zofia", LicenseNumber = "C/1111" };
        db.AddRange(truck, drv);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);
        Func<Task> act = () => mgr.CreateOrderAsync("AGD", 1000, truck.Id, drv.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Vehicle not available*");
    }

    [Fact]
    public async Task Create_Order_Rejects_Unavailable_Driver()
    {
        using var db = CreateContext();

        var van = new Van { RegistrationNumber = "KR-UNAVL", MaxLoadKg = 2000, CargoVolume = 9 };
        var drv = new Driver { Name = "Piotr", LicenseNumber = "B/2222", IsAvailable = false };
        db.AddRange(van, drv);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);
        Func<Task> act = () => mgr.CreateOrderAsync("Towar", 500, van.Id, drv.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Driver not available*");
    }

    [Fact]
    public async Task Create_Order_Allows_Weight_Equal_To_Capacity()
    {
        using var db = CreateContext();

        var van = new Van { RegistrationNumber = "CAP-OK", MaxLoadKg = 1200, CargoVolume = 10 };
        var drv = new Driver { Name = "Ela", LicenseNumber = "B/3333" };
        db.AddRange(van, drv);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);
        var order = await mgr.CreateOrderAsync("Elektronika", 1200, van.Id, drv.Id);

        order.Weight.Should().Be(1200);
        (await db.Vehicles.FindAsync(van.Id))!.IsAvailable.Should().BeFalse();
        (await db.Drivers.FindAsync(drv.Id))!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Complete_Order_Is_Idempotent()
    {
        using var db = CreateContext();

        var truck = new Truck { RegistrationNumber = "PO-IDEMP", MaxLoadKg = 15000, TrailerLength = 12 };
        var drv   = new Driver { Name = "Ida", LicenseNumber = "C/4444" };
        db.AddRange(truck, drv);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);
        var order = await mgr.CreateOrderAsync("Materiały", 5000, truck.Id, drv.Id);

        await mgr.CompleteOrderAsync(order.Id);
        await mgr.CompleteOrderAsync(order.Id); 

        (await db.Orders.FindAsync(order.Id))!.IsCompleted.Should().BeTrue();
        (await db.Vehicles.FindAsync(truck.Id))!.IsAvailable.Should().BeTrue();
        (await db.Drivers.FindAsync(drv.Id))!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Vehicle_RegistrationNumber_Must_Be_Unique()
    {
        using var db = CreateContext();

        db.Vehicles.Add(new Truck { RegistrationNumber = "DUP-111", MaxLoadKg = 10000, TrailerLength = 10 });
        await db.SaveChangesAsync();

        db.Vehicles.Add(new Van { RegistrationNumber = "DUP-111", MaxLoadKg = 1500, CargoVolume = 8 });
        Func<Task> act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(); // unikalny indeks na RegistrationNumber
    }

    [Fact]
    public async Task GetInfo_Dispatches_By_Type()
    {
        using var db = CreateContext();

        var truck = new Truck { RegistrationNumber = "TRK-1", MaxLoadKg = 18000, TrailerLength = 13.6 };
        var van   = new Van   { RegistrationNumber = "VAN-1", MaxLoadKg = 1200,  CargoVolume = 10 };
        db.AddRange(truck, van);
        await db.SaveChangesAsync();

        var t = await db.Vehicles.OfType<Truck>().FirstAsync();
        var v = await db.Vehicles.OfType<Van>().FirstAsync();

        t.GetInfo().Should().Contain("Truck").And.Contain("TRK-1");
        v.GetInfo().Should().Contain("Van").And.Contain("VAN-1");
    }

    [Fact]
    public async Task GetAvailableVehicles_FilterByMinLoad_RespectsAvailability()
    {
        using var db = CreateContext();

        db.Vehicles.AddRange(
            new Van   { RegistrationNumber = "OK-1", MaxLoadKg = 900,  CargoVolume = 6,  IsAvailable = true },
            new Van   { RegistrationNumber = "OK-2", MaxLoadKg = 1500, CargoVolume = 8,  IsAvailable = true },
            new Truck { RegistrationNumber = "BUSY-1", MaxLoadKg = 20000, TrailerLength = 12, IsAvailable = false }
        );
        await db.SaveChangesAsync();

        var filtered = await db.Vehicles.GetAvailableVehicles(1000).ToListAsync();

        filtered.Should().OnlyContain(v => v.IsAvailable && v.MaxLoadKg >= 1000);
        filtered.Select(v => v.RegistrationNumber).Should().Contain("OK-2")
                 .And.NotContain("OK-1")
                 .And.NotContain("BUSY-1");
    }

    [Fact]
    public async Task Create_Order_Throws_When_Vehicle_Not_Found()
    {
        using var db = CreateContext();

        var drv = new Driver { Name = "Ghost Driver", LicenseNumber = "X/9999" };
        db.Drivers.Add(drv);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);
        Func<Task> act = () => mgr.CreateOrderAsync("Towar", 100, vehicleId: 999, driverId: drv.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Vehicle not found*");
    }

    [Fact]
    public async Task Create_Order_Throws_When_Driver_Not_Found()
    {
        using var db = CreateContext();

        var van = new Van { RegistrationNumber = "EX-404", MaxLoadKg = 800, CargoVolume = 5 };
        db.Vehicles.Add(van);
        await db.SaveChangesAsync();

        var mgr = new FleetManager(db);
        Func<Task> act = () => mgr.CreateOrderAsync("Towar", 100, vehicleId: van.Id, driverId: 999);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*Driver not found*");
    }

}
