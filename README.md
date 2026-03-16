Brewery API
Overview
Brewery API is an ASP.NET Core Web API for querying open breweries.
Project Structure
```text
BreweryApi.sln
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
Token Generation:
A helper is included to generate a local JWT for testing protected endpoints.
Token settings
The token must use the same values configured in appsettings.json under Jwt:
Issuer
Audience
SecretKey
TokenExpirationMinutes
The generated token should include the claim:
scope = brewery.read
Generate a token
Use the helper in JwtTokenGenerator.cs with values that match your Jwt configuration.

Run the API project:
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
API Endpoints
Breweries
```http
GET /api/v1/breweries
```
Autocomplete
```http
GET /api/v1/breweries/autocomplete?term=lag
```
Authentication
The API uses JWT bearer authentication.
To call protected endpoints from Swagger:
Generate or provide a valid JWT
Click Authorize in Swagger
Paste the bearer token
Execute the request
