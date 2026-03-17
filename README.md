## Brewery API
### Overview
Brewery API is an ASP.NET Core Web API for querying open breweries.
Project Structure
```text
BreweryApi.slnx
│
├── BreweryApi/                  ASP.NET Core Web API project
├── BreweryApi.Application/      Application layer
├── BreweryApi.Domain/           Domain layer
├── BreweryApi.Infrastructure/   Infrastructure layer
├── BreweryApi.UnitTests/        Unit tests
└── BreweryApi.IntegrationTests/ Integration tests
```
`BreweryApi`
Contains:
Controllers
Middleware
Swagger configuration
Authentication/authorization configuration
Application startup (`Program.cs`)
`BreweryApi.Application`
Contains:
Interfaces
Services
Request models
Response models
`BreweryApi.Domain`
Contains:
Entities
Enums
Value objects
`BreweryApi.Infrastructure`
Contains:
External API client
Cache implementation
Mapping implementation
Configuration/options classes
Supporting service implementations
`BreweryApi.UnitTests`
Contains unit tests for application logic.
`BreweryApi.IntegrationTests`
Contains integration tests for controller, middleware, and authentication behavior.
Requirements
.NET 8 SDK
Configuration
Example `appsettings.json`:
```json
{
  "OpenBreweryDb": {
    "BaseUrl": "https://api.openbrewerydb.org/"
  },
  "Jwt": {
    "Issuer": "BreweryApi",
    "Audience": "BreweryApiClient",
    "SecretKey": "super-secret-key-that-is-at-least-32-characters-long",
    "TokenExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
Running the Project
Restore dependencies:
```bash
dotnet restore
```
Build the solution:
```bash
dotnet build
```
## Run the API project:
```bash
dotnet run --project BreweryApi
```
After the API starts, open the Swagger UI using the local URL shown in the console output.
Running Tests
Run all tests:
```bash
dotnet test
```
Run only unit tests:
```bash
dotnet test BreweryApi.UnitTests
```
Run only integration tests:
```bash
dotnet test BreweryApi.IntegrationTests
```
## Design Decisions

### Layered structure

The solution is split into **API**, **Application**, **Domain**, and **Infrastructure** projects to keep responsibilities separated and make the code easier to test and maintain.

### External API wrapper

The API does not expose Open Brewery DB directly. Instead, it adds:

- filtering to open breweries only
- validation
- search and sort rules
- caching
- authentication
- a simplified response contract

### In-memory caching

The exercise requested in-memory storage, so `IMemoryCache` is used with a **10-minute cache duration**.

### Authentication

JWT bearer authentication is used to secure the endpoints. Authorization uses a policy requiring the `brewery.read` scope.

### Error handling

A global exception middleware catches unhandled exceptions and converts them into consistent HTTP error responses.

### Autocomplete

Autocomplete is implemented as a separate endpoint that returns lightweight brewery suggestions for a partial search term.

## Token Generation:
A helper is included to generate a local JWT for testing protected endpoints.
Token settings
The token must use the same values configured in appsettings.json under Jwt:
- Issuer
- Audience
- SecretKey
- TokenExpirationMinutes
  
The generated token should include the claim:
scope = brewery.read
Generate a token
Use the helper in JwtTokenGenerator.cs with values that match your Jwt configuration.
## API Endpoints
Breweries
```http
GET /api/v1/breweries
```
Autocomplete
```http
GET /api/v1/breweries/autocomplete?term=lag
```
## Authentication
The API uses JWT bearer authentication.
To call protected endpoints from Swagger:
Generate or provide a valid JWT
Click Authorize in Swagger
Paste the bearer token
Execute the request
## Notes
For this project, the JWT secret is stored in configuration for simplicity. In a production environment, secrets should be stored in environment variables or a managed secret store.
