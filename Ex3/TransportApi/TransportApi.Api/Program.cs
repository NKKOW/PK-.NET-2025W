using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TransportApi.Api.Data;
using TransportApi.Api.Extensions;
using TransportApi.Api.Models;
using TransportApi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

//EF Core (SQLite)
builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseSqlite("Data Source=fleet.db"));

//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Transport API",
        Version = "v1",
        Description = "Zarządzanie flotą pojazdów, kierowcami i zleceniami transportowymi."
    });
});

builder.Services.AddScoped<FleetManager>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (ctx, next) =>
{
    var fleet = ctx.RequestServices.GetRequiredService<FleetManager>();
    void Handler(string msg) => app.Logger.LogInformation("[EVENT] {Message}", msg);
    fleet.OnNewOrderCreated += Handler;

    try { await next(); }
    finally { fleet.OnNewOrderCreated -= Handler; }
});

app.MapGet("/", () => Results.Redirect("/swagger"));

//vehicles

//get
app.MapGet("/api/vehicles", async (FleetDbContext db) =>
{
    var data = await db.Vehicles
        .OrderBy(v => v.RegistrationNumber)
        .Select(v => new
        {
            v.Id,
            v.RegistrationNumber,
            v.MaxLoadKg,
            v.IsAvailable,
            Info = v.GetInfo()
        })
        .ToListAsync();
    return Results.Ok(data);
})
.WithName("GetVehicles")
.WithSummary("Pobierz wszystkie pojazdy")
.WithDescription("Zwraca listę wszystkich pojazdów (zarówno dostępnych, jak i zajętych).");

//get
app.MapGet("/api/vehicles/available", async (FleetDbContext db, double? minLoadKg) =>
{
    var data = await db.Vehicles
        .GetAvailableVehicles(minLoadKg) // metoda rozszerzająca
        .OrderBy(v => v.RegistrationNumber)
        .Select(v => new
        {
            v.Id,
            v.RegistrationNumber,
            v.MaxLoadKg,
            v.IsAvailable,
            Info = v.GetInfo()
        })
        .ToListAsync();
    return Results.Ok(data);
})
.WithName("GetAvailableVehicles")
.WithSummary("Pobierz dostępne pojazdy")
.WithDescription("Zwraca listę dostępnych pojazdów. Opcjonalnie filtruje po minimalnym udźwigu (minLoadKg).")
.WithOpenApi(op =>
{
    op.Parameters[0].Description = "Minimalny udźwig w kg (opcjonalnie)";
    return op;
});

//post
app.MapPost("/api/vehicles", async (FleetDbContext db, VehicleCreateDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Type))
        return Results.BadRequest("Type is required ('truck' lub 'van').");

    Vehicle? vehicle = dto.Type.ToLower() switch
    {
        "truck" => new Truck
        {
            RegistrationNumber = dto.RegistrationNumber,
            MaxLoadKg = dto.MaxLoadKg,
            TrailerLength = dto.TrailerLength ?? 0
        },
        "van" => new Van
        {
            RegistrationNumber = dto.RegistrationNumber,
            MaxLoadKg = dto.MaxLoadKg,
            CargoVolume = dto.CargoVolume ?? 0
        },
        _ => null
    };

    if (vehicle is null)
        return Results.BadRequest("Unknown vehicle type. Use 'truck' or 'van'.");

    db.Vehicles.Add(vehicle);
    await db.SaveChangesAsync();
    return Results.Created($"/api/vehicles/{vehicle.Id}", vehicle);
})
.WithName("CreateVehicle")
.WithSummary("Dodaj pojazd")
.WithDescription("Tworzy nowy pojazd typu 'truck' lub 'van'.")
.WithOpenApi(op =>
{
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content =
        {
            ["application/json"] = new Microsoft.OpenApi.Models.OpenApiMediaType
            {
                Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["type"] = new Microsoft.OpenApi.Any.OpenApiString("truck"),
                    ["registrationNumber"] = new Microsoft.OpenApi.Any.OpenApiString("WX12345"),
                    ["maxLoadKg"] = new Microsoft.OpenApi.Any.OpenApiDouble(24000),
                    ["trailerLength"] = new Microsoft.OpenApi.Any.OpenApiDouble(13.6),
                    ["cargoVolume"] = new Microsoft.OpenApi.Any.OpenApiNull()
                }
            }
        }
    };
    return op;
});

//put
app.MapPut("/api/vehicles/{id:int}", async (FleetDbContext db, int id, VehicleUpdateDto dto) =>
{
    var vehicle = await db.Vehicles.FindAsync(id);
    if (vehicle is null) return Results.NotFound();

    vehicle.RegistrationNumber = dto.RegistrationNumber;
    vehicle.MaxLoadKg = dto.MaxLoadKg;

    switch (vehicle)
    {
        case Truck t:
            if (dto.TrailerLength is not null)
                t.TrailerLength = dto.TrailerLength.Value;
            break;

        case Van v:
            if (dto.CargoVolume is not null)
                v.CargoVolume = dto.CargoVolume.Value;
            break;
    }

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("UpdateVehicle")
.WithSummary("Zaktualizuj pojazd")
.WithDescription("Aktualizuje podstawowe informacje o pojeździe (rejestracja, udźwig, długość naczepy / objętość).");

//dele
app.MapDelete("/api/vehicles/{id:int}", async (FleetDbContext db, int id) =>
{
    var vehicle = await db.Vehicles.FindAsync(id);
    if (vehicle is null) return Results.NotFound();

    db.Vehicles.Remove(vehicle);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteVehicle")
.WithSummary("Usuń pojazd")
.WithDescription("Usuwa pojazd z floty.");

//drivers
app.MapGet("/api/drivers", async (FleetDbContext db) =>
    Results.Ok(await db.Drivers.OrderBy(d => d.Name).ToListAsync()))
.WithName("GetDrivers")
.WithSummary("Pobierz kierowców")
.WithDescription("Zwraca listę kierowców.");

//post
app.MapPost("/api/drivers", async (FleetDbContext db, Driver driver) =>
{
    if (string.IsNullOrWhiteSpace(driver.Name) || string.IsNullOrWhiteSpace(driver.LicenseNumber))
        return Results.BadRequest("Name i LicenseNumber są wymagane.");

    db.Drivers.Add(driver);
    await db.SaveChangesAsync();
    return Results.Created($"/api/drivers/{driver.Id}", driver);
})
.WithName("CreateDriver")
.WithSummary("Dodaj kierowcę")
.WithDescription("Rejestruje nowego kierowcę.")
.WithOpenApi(op =>
{
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content =
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["name"] = new Microsoft.OpenApi.Any.OpenApiString("Jan Kowalski"),
                    ["licenseNumber"] = new Microsoft.OpenApi.Any.OpenApiString("C+E/1234"),
                    ["isAvailable"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true)
                }
            }
        }
    };
    return op;
});

//put
app.MapPut("/api/drivers/{id:int}", async (FleetDbContext db, int id, DriverUpdateDto dto) =>
{
    var driver = await db.Drivers.FindAsync(id);
    if (driver is null) return Results.NotFound();

    driver.Name = dto.Name;
    driver.LicenseNumber = dto.LicenseNumber;
    driver.IsAvailable = dto.IsAvailable;

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("UpdateDriver")
.WithSummary("Zaktualizuj kierowcę")
.WithDescription("Aktualizuje dane kierowcy (imię, numer prawa jazdy, dostępność).");

//del
app.MapDelete("/api/drivers/{id:int}", async (FleetDbContext db, int id) =>
{
    var driver = await db.Drivers.FindAsync(id);
    if (driver is null) return Results.NotFound();

    db.Drivers.Remove(driver);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteDriver")
.WithSummary("Usuń kierowcę")
.WithDescription("Usuwa kierowcę z systemu.");

//get
app.MapGet("/api/orders", async (FleetDbContext db) =>
{
    var orders = await db.Orders
        .Where(o => !o.IsCompleted) // tylko aktywne
        .Include(o => o.Vehicle)
        .Include(o => o.Driver)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();
    return Results.Ok(orders);
})
.WithName("GetOrders")
.WithSummary("Pobierz aktywne zlecenia")
.WithDescription("Zwraca wszystkie aktywne (niezakończone) zlecenia, od najnowszych.");

//get
app.MapGet("/api/orders/all", async (FleetDbContext db) =>
{
    var orders = await db.Orders
        .Include(o => o.Vehicle)
        .Include(o => o.Driver)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();
    return Results.Ok(orders);
})
.WithName("GetAllOrders")
.WithSummary("Pobierz wszystkie zlecenia")
.WithDescription("Zwraca wszystkie zlecenia, zarówno aktywne, jak i zakończone.");

//post
app.MapPost("/api/orders", async (FleetManager fleet, OrderCreateDto dto) =>
{
    try
    {
        var order = await fleet.CreateOrderAsync(dto.CargoDescription, dto.Weight, dto.VehicleId, dto.DriverId);
        return Results.Created($"/api/orders/{order.Id}", order);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateOrder")
.WithSummary("Utwórz zlecenie")
.WithDescription("Tworzy zlecenie powiązane z podanym pojazdem i kierowcą.")
.WithOpenApi(op =>
{
    op.RequestBody = new OpenApiRequestBody
    {
        Required = true,
        Content =
        {
            ["application/json"] = new OpenApiMediaType
            {
                Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["cargoDescription"] = new Microsoft.OpenApi.Any.OpenApiString("Elektronika - ładunek 1.2t"),
                    ["weight"] = new Microsoft.OpenApi.Any.OpenApiDouble(1200),
                    ["vehicleId"] = new Microsoft.OpenApi.Any.OpenApiInteger(1),
                    ["driverId"] = new Microsoft.OpenApi.Any.OpenApiInteger(1)
                }
            }
        }
    };
    return op;
});

app.MapPut("/api/orders/{id:int}/complete", async (FleetManager fleet, int id) =>
{
    try
    {
        await fleet.CompleteOrderAsync(id);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CompleteOrder")
.WithSummary("Zakończ zlecenie")
.WithDescription("Oznacza zlecenie jako zakończone i zwalnia pojazd oraz kierowcę.");

app.MapDelete("/api/orders/{id:int}", async (FleetDbContext db, int id) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    if (!order.IsCompleted)
        return Results.BadRequest(new { error = "Nie można usunąć aktywnego zlecenia. Najpierw je zakończ." });

    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteOrder")
.WithSummary("Usuń zlecenie")
.WithDescription("Usuwa zakończone zlecenie z systemu.");

//demo seed
app.MapPost("/api/seed", async (FleetDbContext db) =>
{
    if (!await db.Drivers.AnyAsync())
        db.Drivers.AddRange(
            new Driver { Name = "Jan Kowalski", LicenseNumber = "C+E/1234" },
            new Driver { Name = "Anna Nowak", LicenseNumber = "B/5678" }
        );

    if (!await db.Vehicles.AnyAsync())
        db.Vehicles.AddRange(
            new Truck { RegistrationNumber = "WX12345", MaxLoadKg = 24000, TrailerLength = 13.6 },
            new Van { RegistrationNumber = "KR11111", MaxLoadKg = 1500, CargoVolume = 10 }
        );

    await db.SaveChangesAsync();
    return Results.Ok(new { seeded = true });
})
.WithName("SeedDemo")
.WithSummary("SEED: dodaj dane przykładowe")
.WithDescription("Dodaje przykładowych kierowców i pojazdy (id zaczynają się od 1).");

app.Run();

//dto
record VehicleCreateDto(
    string Type,
    string RegistrationNumber,
    double MaxLoadKg,
    double? TrailerLength,
    double? CargoVolume
);

record VehicleUpdateDto(
    string RegistrationNumber,
    double MaxLoadKg,
    double? TrailerLength,
    double? CargoVolume
);

record OrderCreateDto(
    string CargoDescription,
    double Weight,
    int VehicleId,
    int DriverId
);

record DriverUpdateDto(
    string Name,
    string LicenseNumber,
    bool IsAvailable
);
