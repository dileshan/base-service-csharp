# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

A template ASP.NET Core (.NET 10, MVC controllers) backend microservice. It demonstrates the conventions new services
should copy: folder layout, request/response models, a typed `HttpClient` service calling an upstream API (Open-Meteo),
a single exception middleware producing a uniform error body, unit + in-memory integration tests, a Dockerfile, CI, and
a Postman smoke collection. No UI, no auth, no database yet.

## Commands

```bash
dotnet build base-service-csharp.slnx
dotnet test base-service-csharp.slnx
# Coverage (same gate CI uses; fails below 100% line coverage)
dotnet test base-service-csharp.slnx /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./coverage/ /p:Threshold=100 /p:ThresholdType=line
./scripts/check-coverage.sh [--html|--open] [threshold]   # same check + total line coverage; --html writes coverage/html/ (PowerShell: scripts/check-coverage.ps1 -Html -Open -Threshold 100)
dotnet run --project Api                         # http://localhost:8080
docker build -t base-service-csharp . && docker run -p 8080:8080 base-service-csharp
npx newman run postman/base-service-csharp.postman_collection.json
```

The solution file is `.slnx` (needs the .NET 10 SDK). If no local SDK is installed, everything can run in
`mcr.microsoft.com/dotnet/sdk:10.0`: `docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test base-service-csharp.slnx`.

## Layout and conventions

- `Api/Controllers/`: thin. Bind the request, call one service method, return the result. No business logic, no try/catch.
- `Api/Models/`: request/response contracts (records, camelCase JSON by default). Upstream API de-serialization models live in
  their own sub-namespace (`Models/OpenMeteo/`) with explicit `[JsonPropertyName]` because upstream uses snake_case.
  Never return upstream models directly; map them in the service.
- `Api/Services/`: business logic and upstream calls. Register with `AddHttpClient<IFoo, Foo>` for typed clients; config
  is bound from `appsettings.json` into an options class (`OpenMeteoOptions`).
- `Api/Repositories/`: placeholder for database access (none yet). Services use repositories; controllers don't.
- `Api/Exceptions/`: `ApiException` base (status code + optional target) with `BadRequestException` (400) and `NotFoundException` (404).
  Add new mapped errors by subclassing `ApiException`.
- `Api/Middleware/ExceptionMiddleware.cs`: first in the pipeline. Converts exceptions to `ErrorResponse`
  `{ "code": int, "message": string, "target": string }`. `ApiException` keeps its status; anything unknown is a 500 with a
  generic message (details only go to the log). `target` defaults to the request path.
- Validation: DataAnnotations on request models. `InvalidModelStateResponseFactory` in `Program.cs` returns the same
  `ErrorResponse` shape (target = camelCased field name(s)). Unmatched routes hit `MapFallback`, which throws `NotFoundException`.
- `Api.Tests/` mirrors `Api/` folders (`Controllers/`, `Services/`, `Middleware/`, `Models/`), plus `Infrastructure/` for shared test helpers:
  - `ApiFactory`: `WebApplicationFactory<Program>` with `IWeatherService` swapped for a strict Moq mock; use it for
    endpoint tests that need routing, binding and middleware.
  - `StubHttpMessageHandler`: fake upstream for testing typed `HttpClient` services without network.
  - Stack: xUnit 2, Moq, coverlet.msbuild. Test names follow `Method_Condition_ExpectedResult`.

## Endpoints

| Method | Path         | Notes |
|--------|--------------|-------|
| GET    | `/health`    | `200 text/plain` body `healthy` |
| GET    | `/myweather` | Query: `latitude` (req, -90..90), `longitude` (req, -180..180), `forecastDays` (1–16, default 10), `temperatureUnit` (`fahrenheit`\|`celsius`), `windSpeedUnit` (`mph`\|`kmh`\|`ms`\|`kn`). Calls Open-Meteo `/v1/forecast` with the same fields as the myweatherapp Postman collection and flattens hourly/daily arrays into lists of objects. Upstream 400 → our 400 with upstream `reason`; other upstream failures → 500. |

## CI

`.github/workflows/main.yml` runs four chained jobs (`needs:`): build → test → coverage (100% line threshold) → docker
(builds the image and uploads it as the `docker-image` artifact tarball; load with `docker load -i base-service-csharp.tar`).
Keep `COVERAGE_THRESHOLD` passing when adding code: add tests in the mirrored folder.

## When adding a new endpoint

1. Request/response records in `Models/` (validation via DataAnnotations).
2. Interface + implementation in `Services/`, registered in `Program.cs`.
3. Thin controller in `Controllers/`.
4. Throw `ApiException` subclasses for expected failures; let everything else bubble to the middleware.
5. Tests: service unit tests + endpoint tests through `ApiFactory`; add a request to the Postman collection.
