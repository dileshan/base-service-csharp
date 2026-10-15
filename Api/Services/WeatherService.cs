using System.Globalization;
using System.Net;
using System.Text.Json;
using Api.Exceptions;
using Api.Models;
using Api.Models.OpenMeteo;

namespace Api.Services;

/// <summary>
/// Calls the Open-Meteo forecast API (the same request myweatherapp makes) and maps the
/// column-oriented response into <see cref="MyWeatherResponse"/>.
/// </summary>
public sealed class WeatherService(HttpClient httpClient) : IWeatherService
{
    public const string CurrentFields =
        "temperature_2m,apparent_temperature,relative_humidity_2m,surface_pressure,dew_point_2m,wind_speed_10m,wind_direction_10m,weather_code";

    public const string HourlyFields =
        "temperature_2m,precipitation_probability,weather_code,wind_speed_10m,wind_direction_10m,wind_gusts_10m";

    public const string DailyFields =
        "temperature_2m_max,temperature_2m_min,precipitation_probability_max,weather_code,wind_speed_10m_max";

    public async Task<MyWeatherResponse> GetForecastAsync(
        MyWeatherRequest request,
        CancellationToken cancellationToken
    )
    {
        using var response = await httpClient.GetAsync(
            BuildForecastUri(request),
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var reason = await ReadErrorReasonAsync(response, cancellationToken);
            throw new BadRequestException(reason ?? "The weather provider rejected the request.");
        }

        // Any other non-success status is unexpected and surfaces as a 500 via the exception middleware.
        response.EnsureSuccessStatusCode();

        var forecast =
            await response.Content.ReadFromJsonAsync<OpenMeteoForecastResponse>(cancellationToken)
            ?? throw new InvalidOperationException(
                "Open-Meteo returned an empty forecast response."
            );

        return Map(forecast);
    }

    public static string BuildForecastUri(MyWeatherRequest request) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"/v1/forecast?latitude={request.Latitude}&longitude={request.Longitude}"
                + $"&current={CurrentFields}&hourly={HourlyFields}&daily={DailyFields}"
                + $"&forecast_days={request.ForecastDays}&temperature_unit={request.TemperatureUnit}"
                + $"&wind_speed_unit={request.WindSpeedUnit}&timezone=auto"
        );

    public static MyWeatherResponse Map(OpenMeteoForecastResponse forecast)
    {
        var current = forecast.Current ?? new OpenMeteoCurrent();
        var hourly = forecast.Hourly ?? new OpenMeteoHourly();
        var daily = forecast.Daily ?? new OpenMeteoDaily();

        var units = new WeatherUnits(
            forecast.CurrentUnits.GetValueOrDefault("temperature_2m", string.Empty),
            forecast.CurrentUnits.GetValueOrDefault("wind_speed_10m", string.Empty),
            forecast.CurrentUnits.GetValueOrDefault("surface_pressure", string.Empty)
        );

        return new MyWeatherResponse(
            forecast.Latitude,
            forecast.Longitude,
            forecast.Timezone,
            forecast.Elevation,
            units,
            new CurrentWeather(
                current.Time,
                current.Temperature2m,
                current.ApparentTemperature,
                current.RelativeHumidity2m,
                current.SurfacePressure,
                current.DewPoint2m,
                current.WindSpeed10m,
                current.WindDirection10m,
                current.WeatherCode
            ),
            hourly
                .Time.Select(
                    (time, i) =>
                        new HourlyWeather(
                            time,
                            At(hourly.Temperature2m, i),
                            At(hourly.PrecipitationProbability, i),
                            At(hourly.WeatherCode, i),
                            At(hourly.WindSpeed10m, i),
                            At(hourly.WindDirection10m, i),
                            At(hourly.WindGusts10m, i)
                        )
                )
                .ToList(),
            daily
                .Time.Select(
                    (date, i) =>
                        new DailyWeather(
                            date,
                            At(daily.Temperature2mMax, i),
                            At(daily.Temperature2mMin, i),
                            At(daily.PrecipitationProbabilityMax, i),
                            At(daily.WeatherCode, i),
                            At(daily.WindSpeed10mMax, i)
                        )
                )
                .ToList()
        );
    }

    private static T? At<T>(List<T?> values, int index)
        where T : struct => index < values.Count ? values[index] : null;

    private static async Task<string?> ReadErrorReasonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<OpenMeteoError>(cancellationToken);
            return string.IsNullOrWhiteSpace(error?.Reason) ? null : error.Reason;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
