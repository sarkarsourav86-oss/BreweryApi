using BreweryApi.Domain.Enums;

namespace BreweryApi.Application.Models.Requests;

public sealed class BreweryQuery
{
    public string? SearchQuery { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }

    public BrewerySortBy SortBy { get; init; } = BrewerySortBy.Name;
    public SortDirection SortDirection { get; init; } = SortDirection.Asc;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}