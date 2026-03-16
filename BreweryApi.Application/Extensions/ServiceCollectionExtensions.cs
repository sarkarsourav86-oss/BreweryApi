using BreweryApi.Application.Interfaces;
using BreweryApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BreweryApi.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBreweryService, BreweryService>();
        return services;
    }
}