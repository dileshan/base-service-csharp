using System.Net;
using Api.Exceptions;
using Api.Models;
using Api.Models.OpenMeteo;
using Api.Services;
using Api.Tests.Infrastructure;

namespace Api.Tests.Services;

public sealed class WeatherServiceTests
{
    private static readonly MyWeatherRequest Request = new()
    {
        Latitude = 39.094393,
        Longitude = -104.869855,
    };

    private static (WeatherService Service, StubHttpMessageHandler Handler) CreateService(
        HttpStatusCode status,
        string body
    )
    {
        var handler = new StubHttpMessageHandler(status, body);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.open-meteo.com"),
        };
        return (new WeatherService(client), handler);
    }

    [Fact]
    public async Task GetForecastAsync_SendsSameQueryAsMyWeatherApp()
    {
        var (service, handler) = CreateService(HttpStatusCode.OK, TestData.OpenMeteoForecastJson);

        await service.GetForecastAsync(Request, CancellationToken.None);

        var uri = handler.LastRequest!.RequestUri!;
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal("/v1/forecast", uri.AbsolutePath);
        Assert.Contains("latitude=39.094393", uri.Query);
        Assert.Contains("longitude=-104.869855", uri.Query);
        Assert.Contains($"current={WeatherService.CurrentFields}", uri.Query);
        Assert.Contains($"hourly={WeatherService.HourlyFields}", uri.Query);
        Assert.Contains($"daily={WeatherService.DailyFields}", uri.Query);
        Assert.Contains("forecast_days=10", uri.Query);
        Assert.Contains("temperature_unit=fahrenheit", uri.Query);
        Assert.Contains("wind_speed_unit=mph", uri.Query);
        Assert.Contains("timezone=auto", uri.Query);
    }

    [Fact]
    public async Task GetForecastAsync_MapsOpenMeteoResponse()
    {
        var (service, _) = CreateService(HttpStatusCode.OK, TestData.OpenMeteoForecastJson);

        var result = await service.GetForecastAsync(Request, CancellationToken.None);

        Assert.Equal("America/Denver", result.Timezone);
        Assert.Equal(1950, result.Elevation);
        Assert.Equal(new WeatherUnits("°F", "mph", "hPa"), result.Units);
        Assert.Equal(
            new CurrentWeather("2026-10-14T16:00", 61.2, 59.8, 30, 805.1, 30.4, 7.5, 180, 2),
            result.Current
        );
        Assert.Equal(2, result.Hourly.Count);
        Assert.Equal(
            new HourlyWeather("2026-10-14T16:00", 61.2, 5, 2, 7.5, 180, 15.1),
            result.Hourly[0]
        );
        // Missing/null array entries map to null rather than throwing.
        Assert.Null(result.Hourly[1].PrecipitationProbability);
        Assert.Null(result.Hourly[1].WindGusts);
        Assert.Equal(
            new DailyWeather("2026-10-14", 66.1, 40.3, 10, 3, 12.4),
            Assert.Single(result.Daily)
        );
    }

    [Fact]
    public void Map_MissingBlocks_ReturnsEmptyCollections()
    {
        var result = WeatherService.Map(new OpenMeteoForecastResponse { Timezone = "GMT" });

        Assert.Empty(result.Hourly);
        Assert.Empty(result.Daily);
        Assert.Equal(string.Empty, result.Current.Time);
        Assert.Equal(new WeatherUnits(string.Empty, string.Empty, string.Empty), result.Units);
    }

    [Fact]
    public async Task GetForecastAsync_Provider400_ThrowsBadRequestWithReason()
    {
        var (service, _) = CreateService(
            HttpStatusCode.BadRequest,
            """{"error": true, "reason": "Latitude must be in range of -90 to 90°."}"""
        );

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GetForecastAsync(Request, CancellationToken.None)
        );

        Assert.Equal("Latitude must be in range of -90 to 90°.", exception.Message);
        Assert.Equal(400, exception.StatusCode);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    public async Task GetForecastAsync_Provider400WithoutReason_ThrowsBadRequestWithDefaultMessage(
        string body
    )
    {
        var (service, _) = CreateService(HttpStatusCode.BadRequest, body);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GetForecastAsync(Request, CancellationToken.None)
        );

        Assert.Equal("The weather provider rejected the request.", exception.Message);
    }

    [Fact]
    public async Task GetForecastAsync_Provider500_ThrowsHttpRequestException()
    {
        var (service, _) = CreateService(HttpStatusCode.InternalServerError, "{}");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetForecastAsync(Request, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetForecastAsync_NullBody_ThrowsInvalidOperationException()
    {
        var (service, _) = CreateService(HttpStatusCode.OK, "null");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetForecastAsync(Request, CancellationToken.None)
        );
    }
}
