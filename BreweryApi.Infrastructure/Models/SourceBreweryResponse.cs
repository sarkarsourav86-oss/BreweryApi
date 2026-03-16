using System.Text.Json.Serialization;

namespace BreweryApi.Infrastructure.Models;

public sealed class SourceBreweryResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; init; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("brewery_type")]
    public string BreweryType { get; init; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }
}