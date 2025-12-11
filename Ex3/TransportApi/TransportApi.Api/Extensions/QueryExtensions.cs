using TransportApi.Api.Models;

namespace TransportApi.Api.Extensions;

public static class QueryExtensions
{
    /// <summary>
    /// Zwraca dostępne pojazdy; opcjonalnie filtruje po wymaganym minimalnym udźwigu.
    /// </summary>
    public static IQueryable<Vehicle> GetAvailableVehicles(this IQueryable<Vehicle> query, double? minLoadKg = null)
    {
        query = query.Where(v => v.IsAvailable);
        if (minLoadKg is > 0) query = query.Where(v => v.MaxLoadKg >= minLoadKg);
        return query;
    }
}