using BreweryApi.Application.Interfaces;
using BreweryApi.Application.Models.Requests;
using BreweryApi.Application.Models.Responses;
using BreweryApi.Domain.Entities;
using BreweryApi.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BreweryApi.Application.Services;

public sealed class BreweryService : IBreweryService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IBreweryProviderClient _providerClient;
    private readonly IBreweryCache _cache;
    private readonly IBreweryMapper _mapper;
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly ILogger<BreweryService> _logger;

    public BreweryService(
        IBreweryProviderClient providerClient,
        IBreweryCache cache,
        IBreweryMapper mapper,
        IDistanceCalculator distanceCalculator,
        ILogger<BreweryService> logger)
    {
        _providerClient = providerClient;
        _cache = cache;
        _mapper = mapper;
        _distanceCalculator = distanceCalculator;
        _logger = logger;
    }

    public async Task<PagedResult<BreweryDto>> SearchAsync(BreweryQuery query, CancellationToken cancellationToken)
    {
        ValidateQuery(query);

        var cacheKey = BuildCacheKey(query);
        var cached = await _cache.GetAsync<PagedResult<BreweryDto>>(cacheKey, cancellationToken);

        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for breweries query: {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogInformation("Cache miss for breweries query: {CacheKey}", cacheKey);

        var breweries = await _providerClient.SearchAsync(
            query.SearchQuery,
            query.City,
            query.State,
            query.Country,
            query.Page,
            query.PageSize,
            query.Latitude,
            query.Longitude,
            cancellationToken);

        var openBreweries = breweries
            .Where(x => x.IsOpen())
            .ToList();

        var mappedItems = openBreweries
            .Select(x => _mapper.Map(x, GetDistanceIfAvailable(query, x)))
            .ToList();

        var sortedItems = ApplySorting(mappedItems, query);

        var result = new PagedResult<BreweryDto>
        {
            Items = sortedItems,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = sortedItems.Count
        };

        await _cache.SetAsync(cacheKey, result, CacheDuration, cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<AutocompleteItemDto>> AutocompleteAsync(string term, CancellationToken cancellationToken)
    {
        term = term?.Trim() ?? string.Empty;

        if (term.Length < 3)
        {
            return Array.Empty<AutocompleteItemDto>();
        }

        var query = new BreweryQuery
        {
            SearchQuery = term,
            Page = 1,
            PageSize = 10,
            SortBy = BrewerySortBy.Name,
            SortDirection = SortDirection.Asc
        };

        var results = await SearchAsync(query, cancellationToken);

        return results.Items
        .Where(x => !string.IsNullOrWhiteSpace(x.Name))
        .Where(x => IsAutocompleteMatch(x.Name, term))
        .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
        .Select(g => g.First())
        .OrderBy(x => GetAutocompleteRank(x.Name, term))
        .ThenBy(x => x.Name)
        .Take(10)
        .Select(x => new AutocompleteItemDto
        {
            Id = x.Id,
            Name = x.Name
        })
        .ToList();
    }

    private static bool IsAutocompleteMatch(string name, string term)
    {
        if (name.StartsWith(term, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return words.Any(word => word.StartsWith(term, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetAutocompleteRank(string name, string term)
    {
        if (name.StartsWith(term, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Any(word => word.StartsWith(term, StringComparison.OrdinalIgnoreCase)))
        {
            return 1;
        }

        return 2;
    }

    private static void ValidateQuery(BreweryQuery query)
    {
        if (query.Page <= 0)
            throw new ArgumentOutOfRangeException(nameof(query.Page), "Page must be greater than zero.");

        if (query.PageSize <= 0 || query.PageSize > 200)
            throw new ArgumentOutOfRangeException(nameof(query.PageSize), "PageSize must be between 1 and 200.");

        if (query.SortBy == BrewerySortBy.Distance &&
            (!query.Latitude.HasValue || !query.Longitude.HasValue))
        {
            throw new ArgumentException("Latitude and longitude are required when sorting by distance.");
        }

        if (!string.IsNullOrWhiteSpace(query.SearchQuery) && query.SearchQuery.Trim().Length < 3)
        {
            throw new ArgumentException("searchQuery must be at least 3 characters.");
        }

        var providedSearchValues = new[]
        {
            query.SearchQuery,
            query.City,
            query.State,
            query.Country
        };

        var providedCount = providedSearchValues.Count(x => !string.IsNullOrWhiteSpace(x));

        if (providedCount > 1)
        {
            throw new ArgumentException("Only one of searchQuery, city, state, or country may be provided.");
        }
    }

    private static string BuildCacheKey(BreweryQuery query)
    {
        return string.Join(":",
            "breweries",
            $"searchQuery={query.SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty}",
            $"city={query.City?.Trim().ToLowerInvariant() ?? string.Empty}",
            $"state={query.State?.Trim().ToLowerInvariant() ?? string.Empty}",
            $"country={query.Country?.Trim().ToLowerInvariant() ?? string.Empty}",
            $"sortBy={query.SortBy}",
            $"sortDirection={query.SortDirection}",
            $"lat={query.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty}",
            $"lon={query.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty}",
            $"page={query.Page}",
            $"pageSize={query.PageSize}");
    }

    private double? GetDistanceIfAvailable(BreweryQuery query, Brewery brewery)
    {
        if (query.Latitude is null || query.Longitude is null || brewery.Latitude is null || brewery.Longitude is null)
            return null;

        return _distanceCalculator.CalculateMiles(
            query.Latitude.Value,
            query.Longitude.Value,
            brewery.Latitude.Value,
            brewery.Longitude.Value);
    }

    private static IReadOnlyList<BreweryDto> ApplySorting(IReadOnlyList<BreweryDto> items, BreweryQuery query)
    {
        IEnumerable<BreweryDto> ordered = query.SortBy switch
        {
            BrewerySortBy.City => query.SortDirection == SortDirection.Desc
                ? items.OrderByDescending(x => x.City)
                : items.OrderBy(x => x.City),

            BrewerySortBy.Distance => query.SortDirection == SortDirection.Desc
                ? items.OrderByDescending(x => x.DistanceMiles ?? double.MinValue)
                : items.OrderBy(x => x.DistanceMiles ?? double.MaxValue),

            _ => query.SortDirection == SortDirection.Desc
                ? items.OrderByDescending(x => x.Name)
                : items.OrderBy(x => x.Name)
        };

        return ordered.ToList();
    }
}