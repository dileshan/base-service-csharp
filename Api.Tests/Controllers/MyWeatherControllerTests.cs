using System.Net;
using System.Net.Http.Json;
using Api.Controllers;
using Api.Exceptions;
using Api.Middleware;
using Api.Models;
using Api.Services;
using Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

public sealed class MyWeatherControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public MyWeatherControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.WeatherService.Reset();
    }

    [Fact]
    public async Task Get_ReturnsOkWithServiceResponse()
    {
        var expected = TestData.WeatherResponse();
        var request = new MyWeatherRequest { Latitude = 39.09, Longitude = -104.87 };
        var service = new Mock<IWeatherService>();
        service
            .Setup(s => s.GetForecastAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await new MyWeatherController(service.Object).Get(
            request,
            CancellationToken.None
        );

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetMyWeather_ValidQuery_Returns200AndPassesQueryToService()
    {
        MyWeatherRequest? captured = null;
        _factory
            .WeatherService.Setup(s =>
                s.GetForecastAsync(It.IsAny<MyWeatherRequest>(), It.IsAny<CancellationToken>())
            )
            .Callback<MyWeatherRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(TestData.WeatherResponse());
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(
            "/myweather?latitude=39.09&longitude=-104.87&forecastDays=5&temperatureUnit=celsius"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MyWeatherResponse>();
        Assert.Equal("America/Denver", body?.Timezone);
        Assert.Equal(61.2, body?.Current.Temperature);
        Assert.NotNull(captured);
        Assert.Equal(39.09, captured.Latitude);
        Assert.Equal(-104.87, captured.Longitude);
        Assert.Equal(5, captured.ForecastDays);
        Assert.Equal("celsius", captured.TemperatureUnit);
        Assert.Equal("mph", captured.WindSpeedUnit);
    }

    [Fact]
    public async Task GetMyWeather_MissingLatitude_Returns400ErrorBody()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/myweather?longitude=-104.87");

        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("latitude", error.Target);
        Assert.Contains("Latitude", error.Message);
    }

    [Theory]
    [InlineData("/myweather?latitude=91&longitude=0", "latitude")]
    [InlineData("/myweather?latitude=0&longitude=181", "longitude")]
    [InlineData("/myweather?latitude=abc&longitude=0", "latitude")]
    [InlineData("/myweather?latitude=0&longitude=0&forecastDays=17", "forecastDays")]
    [InlineData("/myweather?latitude=0&longitude=0&temperatureUnit=kelvin", "temperatureUnit")]
    [InlineData("/myweather?latitude=0&longitude=0&windSpeedUnit=furlongs", "windSpeedUnit")]
    public async Task GetMyWeather_InvalidQuery_Returns400ErrorBody(
        string url,
        string expectedTarget
    )
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(url);

        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(expectedTarget, error.Target);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public async Task GetMyWeather_ServiceThrowsBadRequest_Returns400ErrorBody()
    {
        _factory
            .WeatherService.Setup(s =>
                s.GetForecastAsync(It.IsAny<MyWeatherRequest>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new BadRequestException("Provider rejected the request."));
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/myweather?latitude=0&longitude=0");

        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Provider rejected the request.", error.Message);
        Assert.Equal("/myweather", error.Target);
    }

    [Fact]
    public async Task GetMyWeather_ServiceThrowsUnknownException_Returns500ErrorBody()
    {
        _factory
            .WeatherService.Setup(s =>
                s.GetForecastAsync(It.IsAny<MyWeatherRequest>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new HttpRequestException("connection refused"));
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/myweather?latitude=0&longitude=0");

        var error = await AssertErrorAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal(ExceptionMiddleware.UnexpectedErrorMessage, error.Message);
        Assert.Equal("/myweather", error.Target);
    }

    [Fact]
    public async Task UnknownRoute_Returns404ErrorBody()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/does-not-exist");

        var error = await AssertErrorAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("/does-not-exist", error.Target);
        Assert.Contains("/does-not-exist", error.Message);
    }

    private static async Task<ErrorResponse> AssertErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus
    )
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal((int)expectedStatus, error.Code);
        return error;
    }
}
