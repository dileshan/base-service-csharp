using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Api.Tests.Infrastructure;

/// <summary>
/// Hosts the real pipeline in memory with <see cref="IWeatherService"/> replaced by a mock,
/// so tests exercise routing, model binding and the exception middleware without calling Open-Meteo.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public Mock<IWeatherService> WeatherService { get; } = new(MockBehavior.Strict);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services => services.AddSingleton(WeatherService.Object));
    }
}
