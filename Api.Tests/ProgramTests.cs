using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class ProgramTests
{
    [Fact]
    public void WeatherServiceHttpClient_UsesOpenMeteoOptions()
    {
        // Real registrations (no mocked IWeatherService), so the typed client's configure callback runs.
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("OpenMeteo:BaseUrl", "https://open-meteo.test");
            builder.UseSetting("OpenMeteo:TimeoutSeconds", "3");
        });

        // AddHttpClient<IWeatherService, WeatherService> registers a named client called "IWeatherService".
        using var client = factory
            .Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(IWeatherService));

        Assert.Equal(new Uri("https://open-meteo.test"), client.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(3), client.Timeout);
    }
}
