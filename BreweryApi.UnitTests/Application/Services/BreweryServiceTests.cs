using BreweryApi.Application.Interfaces;
using BreweryApi.Application.Models.Requests;
using BreweryApi.Application.Models.Responses;
using BreweryApi.Application.Services;
using BreweryApi.Domain.Entities;
using BreweryApi.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BreweryApi.UnitTests.Application.Services;

public sealed class BreweryServiceTests
{
    private readonly Mock<IBreweryProviderClient> _providerClientMock = new();
    private readonly Mock<IBreweryCache> _cacheMock = new();
    private readonly Mock<IBreweryMapper> _mapperMock = new();
    private readonly Mock<IDistanceCalculator> _distanceCalculatorMock = new();
    private readonly Mock<ILogger<BreweryService>> _loggerMock = new();

    private readonly BreweryService _sut;

    public BreweryServiceTests()
    {
        _sut = new BreweryService(
            _providerClientMock.Object,
            _cacheMock.Object,
            _mapperMock.Object,
            _distanceCalculatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SearchAsync_CacheHit_ReturnsCachedResult_AndDoesNotCallProvider()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SearchQuery = "ale",
            Page = 1,
            PageSize = 25
        };

        var cached = new PagedResult<BreweryDto>
        {
            Items = new List<BreweryDto>
            {
                new() { Id = "1", Name = "Cached Brewery", City = "Minneapolis" }
            },
            Page = 1,
            PageSize = 25,
            TotalCount = 1
        };

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<BreweryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(cached);

        _providerClientMock.Verify(
            x => x.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<double?>(),
                It.IsAny<double?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PagedResult<BreweryDto>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_CacheMiss_CallsProvider_AndCachesResult()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SearchQuery = "ale",
            Page = 1,
            PageSize = 25
        };

        var breweries = new List<Brewery>
        {
            CreateBrewery("1", "Alpha Brewing", "Denver", "micro")
        };

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<BreweryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<BreweryDto>?)null);

        _providerClientMock
            .Setup(x => x.SearchAsync(
                query.SearchQuery,
                query.City,
                query.State,
                query.Country,
                query.Page,
                query.PageSize,
                query.Latitude,
                query.Longitude,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(breweries);

        _mapperMock
            .Setup(x => x.Map(It.IsAny<Brewery>(), It.IsAny<double?>()))
            .Returns<Brewery, double?>((b, d) => new BreweryDto
            {
                Id = b.Id,
                Name = b.Name,
                City = b.City,
                Phone = b.Phone,
                DistanceMiles = d
            });

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Alpha Brewing");

        _providerClientMock.Verify(
            x => x.SearchAsync(
                query.SearchQuery,
                query.City,
                query.State,
                query.Country,
                query.Page,
                query.PageSize,
                query.Latitude,
                query.Longitude,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PagedResult<BreweryDto>>(),
                It.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(10)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_FiltersOutClosedBreweries()
    {
        // Arrange
        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 25
        };

        var breweries = new List<Brewery>
        {
            CreateBrewery("1", "Open Brewery", "Denver", "micro"),
            CreateBrewery("2", "Closed Brewery", "Denver", "closed")
        };

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<BreweryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<BreweryDto>?)null);

        _providerClientMock
            .Setup(x => x.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<double?>(),
                It.IsAny<double?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(breweries);

        _mapperMock
            .Setup(x => x.Map(It.IsAny<Brewery>(), It.IsAny<double?>()))
            .Returns<Brewery, double?>((b, d) => new BreweryDto
            {
                Id = b.Id,
                Name = b.Name,
                City = b.City,
                Phone = b.Phone,
                DistanceMiles = d
            });

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Single().Name.Should().Be("Open Brewery");

        _mapperMock.Verify(x => x.Map(It.Is<Brewery>(b => b.Name == "Open Brewery"), It.IsAny<double?>()), Times.Once);
        _mapperMock.Verify(x => x.Map(It.Is<Brewery>(b => b.Name == "Closed Brewery"), It.IsAny<double?>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_SortByNameAsc_ReturnsNameOrderedResults()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.Name,
            SortDirection = SortDirection.Asc,
            Page = 1,
            PageSize = 25
        };

        SetupCacheMiss();
        SetupProviderBreweries(
            CreateBrewery("2", "Zulu Brewing", "B City", "micro"),
            CreateBrewery("1", "Alpha Brewing", "A City", "micro"));

        SetupMapper();

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Name).Should().Equal("Alpha Brewing", "Zulu Brewing");
    }

    [Fact]
    public async Task SearchAsync_SortByNameDesc_ReturnsNameOrderedResults()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.Name,
            SortDirection = SortDirection.Desc,
            Page = 1,
            PageSize = 25
        };

        SetupCacheMiss();
        SetupProviderBreweries(
            CreateBrewery("1", "Alpha Brewing", "A City", "micro"),
            CreateBrewery("2", "Zulu Brewing", "B City", "micro"));

        SetupMapper();

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Name).Should().Equal("Zulu Brewing", "Alpha Brewing");
    }

    [Fact]
    public async Task SearchAsync_SortByCityAsc_ReturnsCityOrderedResults()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.City,
            SortDirection = SortDirection.Asc,
            Page = 1,
            PageSize = 25
        };

        SetupCacheMiss();
        SetupProviderBreweries(
            CreateBrewery("1", "Brewery One", "Zulu City", "micro"),
            CreateBrewery("2", "Brewery Two", "Alpha City", "micro"));

        SetupMapper();

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.City).Should().Equal("Alpha City", "Zulu City");
    }

    [Fact]
    public async Task SearchAsync_SortByCityDesc_ReturnsCityOrderedResults()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.City,
            SortDirection = SortDirection.Desc,
            Page = 1,
            PageSize = 25
        };

        SetupCacheMiss();
        SetupProviderBreweries(
            CreateBrewery("1", "Brewery One", "Alpha City", "micro"),
            CreateBrewery("2", "Brewery Two", "Zulu City", "micro"));

        SetupMapper();

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.City).Should().Equal("Zulu City", "Alpha City");
    }

    [Fact]
    public async Task SearchAsync_SortByDistanceAsc_ReturnsDistanceOrderedResults()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.Distance,
            SortDirection = SortDirection.Asc,
            Latitude = 44.98,
            Longitude = -93.26,
            Page = 1,
            PageSize = 25
        };

        var near = CreateBrewery("1", "Near Brewery", "City A", "micro", 44.99, -93.27);
        var far = CreateBrewery("2", "Far Brewery", "City B", "micro", 45.50, -94.00);

        SetupCacheMiss();
        SetupProviderBreweries(far, near);

        _distanceCalculatorMock
            .Setup(x => x.CalculateMiles(query.Latitude.Value, query.Longitude.Value, near.Latitude!.Value, near.Longitude!.Value))
            .Returns(5);

        _distanceCalculatorMock
            .Setup(x => x.CalculateMiles(query.Latitude.Value, query.Longitude.Value, far.Latitude!.Value, far.Longitude!.Value))
            .Returns(50);

        SetupMapper();

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Name).Should().Equal("Near Brewery", "Far Brewery");
    }

    [Fact]
    public async Task SearchAsync_SortByDistanceDesc_ReturnsDistanceOrderedResults()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.Distance,
            SortDirection = SortDirection.Desc,
            Latitude = 44.98,
            Longitude = -93.26,
            Page = 1,
            PageSize = 25
        };

        var near = CreateBrewery("1", "Near Brewery", "City A", "micro", 44.99, -93.27);
        var far = CreateBrewery("2", "Far Brewery", "City B", "micro", 45.50, -94.00);

        SetupCacheMiss();
        SetupProviderBreweries(near, far);

        _distanceCalculatorMock
            .Setup(x => x.CalculateMiles(query.Latitude.Value, query.Longitude.Value, near.Latitude!.Value, near.Longitude!.Value))
            .Returns(5);

        _distanceCalculatorMock
            .Setup(x => x.CalculateMiles(query.Latitude.Value, query.Longitude.Value, far.Latitude!.Value, far.Longitude!.Value))
            .Returns(50);

        SetupMapper();

        // Act
        var result = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Name).Should().Equal("Far Brewery", "Near Brewery");
    }

    [Fact]
    public async Task SearchAsync_MultipleSearchInputs_ThrowsArgumentException()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SearchQuery = "ale",
            City = "Fargo",
            Page = 1,
            PageSize = 25
        };

        // Act
        var act = () => _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Only one of searchQuery, city, state, or country may be provided.");
    }

    [Fact]
    public async Task SearchAsync_SortByDistanceWithoutLatitudeLongitude_ThrowsArgumentException()
    {
        // Arrange
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.Distance,
            SortDirection = SortDirection.Asc,
            Page = 1,
            PageSize = 25
        };

        // Act
        var act = () => _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Latitude and longitude are required when sorting by distance.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchAsync_InvalidPage_ThrowsArgumentOutOfRangeException(int page)
    {
        // Arrange
        var query = new BreweryQuery
        {
            Page = page,
            PageSize = 25
        };

        // Act
        var act = () => _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("Page must be greater than zero.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(201)]
    public async Task SearchAsync_InvalidPageSize_ThrowsArgumentOutOfRangeException(int pageSize)
    {
        // Arrange
        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = pageSize
        };

        // Act
        var act = () => _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("PageSize must be between 1 and 200.*");
    }

    [Fact]
    public async Task AutocompleteAsync_BlankTerm_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.AutocompleteAsync("   ", CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AutocompleteAsync_ReturnsDistinctTopTenNames()
    {
        // Arrange
        var queryResult = new PagedResult<BreweryDto>
        {
            Items = Enumerable.Range(1, 12)
                .Select(i => new BreweryDto
                {
                    Id = i.ToString(),
                    Name = i <= 2 ? "Duplicate Brewery" : $"Brewery {i}",
                    City = "City"
                })
                .ToList(),
            Page = 1,
            PageSize = 10,
            TotalCount = 12
        };

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<BreweryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        // Act
        var result = await _sut.AutocompleteAsync("brew", CancellationToken.None);

        // Assert
        result.Should().HaveCount(10);
        result.Select(x => x.Name).Should().OnlyHaveUniqueItems();
    }

    private void SetupCacheMiss()
    {
        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<BreweryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<BreweryDto>?)null);
    }

    private void SetupProviderBreweries(params Brewery[] breweries)
    {
        _providerClientMock
            .Setup(x => x.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<double?>(),
                It.IsAny<double?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(breweries.ToList());
    }

    private void SetupMapper()
    {
        _mapperMock
            .Setup(x => x.Map(It.IsAny<Brewery>(), It.IsAny<double?>()))
            .Returns<Brewery, double?>((b, d) => new BreweryDto
            {
                Id = b.Id,
                Name = b.Name,
                City = b.City,
                Phone = b.Phone,
                DistanceMiles = d
            });
    }

    private static Brewery CreateBrewery(
        string id,
        string name,
        string city,
        string breweryType,
        double? latitude = null,
        double? longitude = null)
    {
        return new Brewery(
            id,
            name,
            city,
            "5551234567",
            breweryType,
            latitude,
            longitude);
    }
}