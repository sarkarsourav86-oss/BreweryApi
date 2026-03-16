using BreweryApi.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BreweryApi.IntegrationTests.Infrastructure;

public sealed class AuthenticatedTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IBreweryService _breweryService;

    public AuthenticatedTestWebApplicationFactory(IBreweryService breweryService)
    {
        _breweryService = breweryService;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBreweryService>();
            services.AddSingleton(_breweryService);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { });
        });
    }
}