# base-service-csharp

This ASP.NET Core microservice (MVC style, .NET 10) is used as a template or boilerplate code. It contains a basic middleware pipeline, the folder structure to follow, serialization/de-serialization objects, and exception handling. Note this service is backend only and doesn't have any Views or UI. Any UI should be done in React or Vue.js.

As a working example it exposes `GET /myweather`, which calls the [Open-Meteo](https://open-meteo.com/) forecast API (the same request the myweatherapp makes) and returns a simplified response.

What this service doesn't currently have:
- Authorization
- Actual DB connections

## Folder structure
```
-- .github/workflows/main.yml // CI: build -> test -> coverage -> docker image artifact
-- postman // Postman collection used for automated smoke testing.
-- Api.Tests // Mirrors the Api folders.
    -- Controllers // Controller unit tests + in-memory endpoint tests (WebApplicationFactory).
    -- Services
    -- Middleware
    -- Models
    -- Infrastructure // Shared test helpers (ApiFactory, StubHttpMessageHandler, TestData).
-- Api
    -- Controllers // This microservice's endpoint definitions. Should be very small: just call service class code and return the result.
    -- Models // Request and response definitions (upstream API models live in Models/OpenMeteo).
    -- Services // Business logic that connects to another API or pulls in repository classes for database reads and writes.
    -- Repositories // Handles database operations (placeholder, no DB yet).
    -- Exceptions // Custom exceptions (ApiException, BadRequestException, NotFoundException).
    -- Middleware // Custom request pipeline logic, such as exception handling, rate limiting or authorization.
    |-- Program.cs // Main entry point and pipeline definitions.
    |-- appsettings.json
-- Dockerfile
-- base-service-csharp.slnx
```

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Returns `200` with the text body `healthy`. |
| GET | `/myweather` | Current, hourly and daily forecast. Query: `latitude` (required), `longitude` (required), `forecastDays` (1-16, default 10), `temperatureUnit` (`fahrenheit` or `celsius`, default `fahrenheit`), `windSpeedUnit` (`mph`, `kmh`, `ms` or `kn`, default `mph`). |

Example: `GET http://localhost:8080/myweather?latitude=39.094393&longitude=-104.869855`

### Errors

Every error uses the same body, produced by `Middleware/ExceptionMiddleware.cs` (and by model validation):

```json
{ "code": 400, "message": "The Latitude field is required.", "target": "latitude" }
```

| Code | When |
|------|------|
| 400 | Invalid/missing query parameters, or `BadRequestException` (e.g. the upstream API rejected the request). `target` is the offending field. |
| 404 | No route matches, or `NotFoundException`. `target` is the request path. |
| 500 | Any unhandled exception. The message is generic; details are logged only. |

## Running locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project Api        # http://localhost:8080
dotnet test base-service-csharp.slnx
```

Coverage (CI fails under 80% line coverage):

```bash
dotnet test base-service-csharp.slnx /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/ /p:Threshold=80 /p:ThresholdType=line
```

## Docker

```bash
docker build -t base-service-csharp .
docker run -p 8080:8080 base-service-csharp
# OR
docker run -d --name base-service-csharp -p 8080:8080 base-service-csharp:local
```

## Postman

`postman/base-service-csharp.postman_collection.json` calls `/myweather` and `/health` and checks for a `200`. Change the `baseUrl` collection variable if the service isn't on `http://localhost:8080`.

```bash
npx newman run postman/base-service-csharp.postman_collection.json
```

## CI

`.github/workflows/main.yml` runs on pushes to `main`, on pull requests and on manual dispatch. Each job depends on the previous one:

1. **Build**: restore and build the solution.
2. **Test**: run the tests and upload TRX results.
3. **Coverage**: run the tests with coverlet and fail below 80% line coverage. The Cobertura report is uploaded.
4. **Docker image**: build the image and upload it as the `docker-image` artifact (`docker load -i base-service-csharp.tar`).
