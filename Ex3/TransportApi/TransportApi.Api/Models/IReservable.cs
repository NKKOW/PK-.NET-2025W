namespace TransportApi.Api.Models;

public interface IReservable
{
    void AssignDriver(Driver driver);
    void StartOrder();
    void CompleteOrder();
}