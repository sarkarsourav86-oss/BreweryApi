using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using BreweryApi.Application.Interfaces;
using BreweryApi.Domain.Entities;
using BreweryApi.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace BreweryApi.Infrastructure.Clients;

public sealed class OpenBreweryDbClient : IBreweryProviderClient
{
    private static readonly string[] SupportedOpenTypes =
    [
        "micro",
        "nano",
        "regional",
        "brewpub",
        "large",
        "planning",
        "bar",
        "contract",
        "proprietor"
    ];

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenBreweryDbClient> _logger;

    public OpenBreweryDbClient(HttpClient httpClient, ILogger<OpenBreweryDbClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Brewery>> SearchAsync(
        string? searchQuery,
        string? city,
        string? state,
        string? country,
        int page,
        int pageSize,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken)
    {
        var path = BuildPath(searchQuery, city, state, country, page, pageSize, latitude, longitude);

        _logger.LogInformation("Calling Open Brewery DB: {Path}", path);

        using var response = await _httpClient.GetAsync(path, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "Open Brewery DB request failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Open Brewery DB request failed with status code {(int)response.StatusCode}.");
        }

        var sourceItems =
            await response.Content.ReadFromJsonAsync<List<SourceBreweryResponse>>(cancellationToken: cancellationToken)
            ?? [];

        return sourceItems
            .Select(MapToDomain)
            .ToList();
    }

    private static string BuildPath(
        string? searchQuery,
        string? city,
        string? state,
        string? country,
        int page,
        int pageSize,
        double? latitude,
        double? longitude)
    {
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            return BuildSearchPath(searchQuery, page, pageSize);
        }

        return BuildListPath(city, state, country, page, pageSize, latitude, longitude);
    }

    private static string BuildSearchPath(
        string searchQuery,
        int page,
        int pageSize)
    {
        var queryParams = new List<string>
        {
            $"query={Uri.EscapeDataString(searchQuery.Trim())}",
            $"page={page}",
            $"per_page={pageSize}"
        };

        var sb = new StringBuilder("v1/breweries/search");
        sb.Append('?');
        sb.Append(string.Join("&", queryParams));

        return sb.ToString();
    }

    private static string BuildListPath(
        string? city,
        string? state,
        string? country,
        int page,
        int pageSize,
        double? latitude,
        double? longitude)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"per_page={pageSize}",
            $"by_type={Uri.EscapeDataString(string.Join(",", SupportedOpenTypes))}"
        };

        if (!string.IsNullOrWhiteSpace(city))
        {
            queryParams.Add($"by_city={Uri.EscapeDataString(city.Trim())}");
        }
        else if (!string.IsNullOrWhiteSpace(state))
        {
            queryParams.Add($"by_state={Uri.EscapeDataString(state.Trim())}");
        }
        else if (!string.IsNullOrWhiteSpace(country))
        {
            queryParams.Add($"by_country={Uri.EscapeDataString(country.Trim())}");
        }

        if (latitude.HasValue && longitude.HasValue)
        {
            var byDist =
                $"{latitude.Value.ToString(CultureInfo.InvariantCulture)}," +
                $"{longitude.Value.ToString(CultureInfo.InvariantCulture)}";

            queryParams.Add($"by_dist={Uri.EscapeDataString(byDist)}");
        }

        var sb = new StringBuilder("v1/breweries");
        sb.Append('?');
        sb.Append(string.Join("&", queryParams));

        return sb.ToString();
    }

    private static Brewery MapToDomain(SourceBreweryResponse source)
    {
        return new Brewery(
            source.Id,
            source.Name,
            source.City,
            source.Phone,
            source.BreweryType,
            source.Latitude,
            source.Longitude);
    }
}