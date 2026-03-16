using BreweryApi.Domain.Entities;

namespace BreweryApi.Application.Interfaces;

public interface IBreweryProviderClient
{
    Task<IReadOnlyList<Brewery>> SearchAsync(
        string? searchQuery,
        string? city,
        string? state,
        string? country,
        int page,
        int pageSize,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken);
}