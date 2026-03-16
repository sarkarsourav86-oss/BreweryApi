using System.Net;
using System.Net.Http.Json;
using BreweryApi.Application.Interfaces;
using BreweryApi.Application.Models.Requests;
using BreweryApi.Application.Models.Responses;
using BreweryApi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Moq;
using Xunit;

namespace BreweryApi.IntegrationTests.Controllers;

public sealed class BreweriesControllerIntegrationTests
{
    [Fact]
    public async Task GetBreweries_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        await using var factory = new TestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries?searchQuery=ale&page=1&pageSize=25");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBreweries_ValidRequest_ReturnsOk()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        var expected = new PagedResult<BreweryDto>
        {
            Items = new List<BreweryDto>
            {
                new()
                {
                    Id = "1",
                    Name = "Alpha Brewing",
                    City = "Denver",
                    Phone = "5551234567",
                    DistanceMiles = null
                }
            },
            Page = 1,
            PageSize = 25,
            TotalCount = 1
        };

        breweryServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        await using var factory = new AuthenticatedTestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries?searchQuery=ale&page=1&pageSize=25");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<BreweryDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Alpha Brewing");
    }

    [Fact]
    public async Task GetBreweries_InvalidQuery_ReturnsBadRequest()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        breweryServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Only one of searchQuery, city, state, or country may be provided."));

        await using var factory = new AuthenticatedTestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries?city=fargo&state=minnesota");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Bad Request");
        body.Should().Contain("Only one of searchQuery, city, state, or country may be provided.");
    }

    [Fact]
    public async Task GetBreweries_UpstreamFailure_ReturnsBadGateway()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        breweryServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Open Brewery DB request failed with status code 500."));

        await using var factory = new AuthenticatedTestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries?searchQuery=ale");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Bad Gateway");
        body.Should().Contain("Open Brewery DB request failed with status code 500.");
    }

    [Fact]
    public async Task GetBreweries_UnexpectedFailure_ReturnsInternalServerError()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        breweryServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Boom"));

        await using var factory = new AuthenticatedTestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries?searchQuery=ale");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Internal Server Error");
        body.Should().Contain("An unexpected error occurred.");
    }

    [Fact]
    public async Task GetBreweries_InvalidSortBy_ReturnsBadRequest()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        await using var factory = new AuthenticatedTestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries?sortBy=banana");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Bad Request");
        body.Should().Contain("sortBy must be one of: name, city, distance.");
    }

    [Fact]
    public async Task Autocomplete_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        await using var factory = new TestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries/autocomplete?term=lag");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Autocomplete_ValidRequest_ReturnsOk()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        var expected = new List<AutocompleteItemDto>
        {
            new() { Id = "Lagunitas Brewing Company", Name = "Lagunitas Brewing Company" },
            new() { Id = "Lager House Brewery", Name = "Lager House Brewery" }
        };

        breweryServiceMock
            .Setup(x => x.AutocompleteAsync("lag", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        await using var factory = new AuthenticatedTestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries/autocomplete?term=lag");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<AutocompleteItemDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result[0].Name.Should().Be("Lagunitas Brewing Company");
    }

    [Fact]
    public async Task Autocomplete_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        breweryServiceMock
            .Setup(x => x.AutocompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("term is invalid."));

        await using var factory = new AuthenticatedTestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries/autocomplete?term=abc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Bad Request");
        body.Should().Contain("term is invalid.");
    }

    [Fact]
    public async Task Autocomplete_MissingTerm_ReturnsFrameworkValidationBadRequest()
    {
        // Arrange
        var breweryServiceMock = new Mock<IBreweryService>();

        await using var factory = new AuthenticatedTestWebApplicationFactory(breweryServiceMock.Object);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/breweries/autocomplete");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("One or more validation errors occurred.");
        body.Should().Contain("term");
    }
}