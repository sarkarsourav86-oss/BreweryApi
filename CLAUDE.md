# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
dotnet restore              # Restore dependencies
dotnet build                # Build entire solution
dotnet run --project BreweryApi  # Run the API (Swagger UI available in Development)
dotnet test                 # Run all tests
dotnet test BreweryApi.UnitTests          # Unit tests only
dotnet test BreweryApi.IntegrationTests   # Integration tests only
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"  # Run a single test
```

Requires **.NET 8 SDK**.

## Architecture

Clean/layered architecture with four main projects and a helper:

- **BreweryApi** (Web API host) — Controllers, middleware, JWT auth setup, Swagger config, `Program.cs` entry point. Uses URL-segment API versioning (`/api/v1/`).
- **BreweryApi.Application** — Business logic layer. `BreweryService` is the core service: handles search with validation, caching (10-min TTL via `IBreweryCache`), open-brewery filtering, distance calculation, and sorting. Autocomplete delegates to search internally.
- **BreweryApi.Domain** — Entities (`Brewery`), enums (`BrewerySortBy`, `SortDirection`), value objects (`Coordinates`). No dependencies on other projects.
- **BreweryApi.Infrastructure** — `OpenBreweryDbClient` wraps the Open Brewery DB REST API, `MemoryBreweryCache` implements in-memory caching, `BreweryMapper` maps domain to DTOs, `DistanceCalculator` computes distances.
- **GenerateToken** — Console helper to generate JWT tokens for manual API testing.

Dependency flow: `BreweryApi` → `Application` → `Domain` ← `Infrastructure`. Each layer registers its own DI via `ServiceCollectionExtensions.AddApplication()` / `AddInfrastructure()`.

## Key Design Details

- **Only open breweries** are returned — `Brewery.IsOpen()` filters by supported brewery types (micro, nano, regional, brewpub, large, planning, bar, contract, proprietor).
- **Search constraint**: only one of `searchQuery`, `city`, `state`, `country` can be provided per request.
- **Auth**: JWT bearer with `brewery.read` scope claim required. Policy name: `"CanReadBreweries"`. JWT settings in `appsettings.json` under `Jwt` section.
- **Global exception handling**: `ExceptionHandlingMiddleware` catches unhandled exceptions and returns `ErrorResponse` JSON.

## Testing

- **Unit tests** (xUnit + Moq + FluentAssertions): Mock all `BreweryService` dependencies. Tests live in `BreweryApi.UnitTests/Application/Services/`.
- **Integration tests** (xUnit + `WebApplicationFactory`): Use `TestWebApplicationFactory` which replaces `IBreweryService` with a mock. `AuthenticatedTestWebApplicationFactory` adds a `TestAuthHandler` for bypassing JWT auth in tests.
