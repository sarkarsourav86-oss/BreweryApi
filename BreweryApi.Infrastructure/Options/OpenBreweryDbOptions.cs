namespace BreweryApi.Infrastructure.Options;

public sealed class OpenBreweryDbOptions
{
    public const string SectionName = "OpenBreweryDb";

    public string BaseUrl { get; init; } = "https://api.openbrewerydb.org/";
}