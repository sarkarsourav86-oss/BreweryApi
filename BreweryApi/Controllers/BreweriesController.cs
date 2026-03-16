using Asp.Versioning;
using BreweryApi.Application.Interfaces;
using BreweryApi.Application.Models.Requests;
using BreweryApi.Application.Models.Responses;
using BreweryApi.Domain.Enums;
using BreweryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BreweryApi.Controllers;

/// <summary>
/// Endpoints for querying open breweries.
/// </summary>
/// <remarks>
/// All brewery endpoints require a valid JWT bearer token with the <c>brewery.read</c> scope.
/// </remarks>
[Authorize(Policy = "CanReadBreweries")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/breweries")]
public sealed class BreweriesController : ControllerBase
{
    private readonly IBreweryService _breweryService;

    public BreweriesController(IBreweryService breweryService)
    {
        _breweryService = breweryService;
    }

    /// <summary>
    /// Returns a paged list of open breweries.
    /// </summary>
    /// <remarks>
    /// Supported search inputs:
    ///
    /// - <c>searchQuery</c>: free-text brewery search
    /// - <c>city</c>: filter by city
    /// - <c>state</c>: filter by state
    /// - <c>country</c>: filter by country
    ///
    /// Only one of <c>searchQuery</c>, <c>city</c>, <c>state</c>, or <c>country</c> may be provided.
    ///
    /// Supported sort values:
    ///
    /// - <c>name</c>
    /// - <c>city</c>
    /// - <c>distance</c>
    ///
    /// If <c>sortBy=distance</c>, both <c>latitude</c> and <c>longitude</c> are required.
    ///
    /// Results are cached in memory for 10 minutes.
    /// </remarks>
    /// <param name="searchQuery">Free-text search term for brewery lookup.</param>
    /// <param name="city">City-based filter. Only one search input may be provided.</param>
    /// <param name="state">State-based filter. Only one search input may be provided.</param>
    /// <param name="country">Country-based filter. Only one search input may be provided.</param>
    /// <param name="sortBy">Sort field: name, city, or distance.</param>
    /// <param name="sortDirection">Sort direction: asc or desc.</param>
    /// <param name="latitude">Latitude used when sorting by distance.</param>
    /// <param name="longitude">Longitude used when sorting by distance.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size, from 1 to 200.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>A paged list of brewery results.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BreweryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBreweries(
        [FromQuery] string? searchQuery,
        [FromQuery] string? city,
        [FromQuery] string? state,
        [FromQuery] string? country,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = new BreweryQuery
        {
            SearchQuery = searchQuery,
            City = city,
            State = state,
            Country = country,
            SortBy = ParseSortBy(sortBy),
            SortDirection = ParseSortDirection(sortDirection),
            Latitude = latitude,
            Longitude = longitude,
            Page = page,
            PageSize = pageSize
        };

        var result = await _breweryService.SearchAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns autocomplete suggestions for brewery names.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT bearer token with the <c>brewery.read</c> scope.
    /// Returns up to 10 distinct suggestions.
    /// </remarks>
    /// <param name="term">Partial text used to retrieve brewery name suggestions.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>A list of autocomplete suggestions.</returns>
    [HttpGet("autocomplete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IReadOnlyList<AutocompleteItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Autocomplete(
        [FromQuery] string term,
        CancellationToken cancellationToken = default)
    {
        var result = await _breweryService.AutocompleteAsync(term, cancellationToken);
        return Ok(result);
    }

    private static BrewerySortBy ParseSortBy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return BrewerySortBy.Name;

        if (Enum.TryParse<BrewerySortBy>(value, true, out var parsed))
            return parsed;

        throw new ArgumentException("sortBy must be one of: name, city, distance.");
    }

    private static SortDirection ParseSortDirection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SortDirection.Asc;

        if (Enum.TryParse<SortDirection>(value, true, out var parsed))
            return parsed;

        throw new ArgumentException("sortDirection must be one of: asc, desc.");
    }
}