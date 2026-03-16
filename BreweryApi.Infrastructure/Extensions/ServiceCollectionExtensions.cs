using BreweryApi.Application.Interfaces;
using BreweryApi.Infrastructure.Caching;
using BreweryApi.Infrastructure.Clients;
using BreweryApi.Infrastructure.Mapping;
using BreweryApi.Infrastructure.Options;
using BreweryApi.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BreweryApi.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenBreweryDbOptions>(
            configuration.GetSection(OpenBreweryDbOptions.SectionName));

        services.AddMemoryCache();

        services.AddScoped<IBreweryCache, MemoryBreweryCache>();
        services.AddScoped<IBreweryMapper, BreweryMapper>();
        services.AddScoped<IDistanceCalculator, DistanceCalculator>();

        services.AddHttpClient<IBreweryProviderClient, OpenBreweryDbClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<OpenBreweryDbOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}