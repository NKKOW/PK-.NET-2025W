namespace TransportApi.Api.Models;

public class Truck : Vehicle
{
    public double TrailerLength { get; set; }

    public override string GetInfo()
        => $"Truck {RegistrationNumber} | MaxLoad: {MaxLoadKg}kg | Trailer: {TrailerLength}m";
}