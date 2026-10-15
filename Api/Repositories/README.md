# Repositories

Database read/write classes live here. This template has no database connection yet; when one is added,
define an interface (e.g. `IWeatherRepository`) and its implementation here, register it in `Program.cs`,
and inject it into the service classes in `Services/`. Controllers should never use repositories directly.
