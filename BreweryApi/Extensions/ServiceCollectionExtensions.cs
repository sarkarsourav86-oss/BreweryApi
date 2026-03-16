using BreweryApi.Application.Interfaces;
using BreweryApi.Application.Services;

namespace BreweryApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
            services.AddScoped<IBreweryService, BreweryService>();
            return services;
        }
    }
}
