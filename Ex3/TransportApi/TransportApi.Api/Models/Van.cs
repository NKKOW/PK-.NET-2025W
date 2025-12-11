namespace TransportApi.Api.Models;

public class Van : Vehicle
{
    public double CargoVolume { get; set; } 

    public override string GetInfo()
        => $"Van {RegistrationNumber} | MaxLoad: {MaxLoadKg}kg | Volume: {CargoVolume}m3";
}