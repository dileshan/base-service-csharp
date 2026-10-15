using Api.Models;

namespace Api.Tests.Infrastructure;

public static class TestData
{
    public static MyWeatherResponse WeatherResponse() =>
        new(
            39.09,
            -104.87,
            "America/Denver",
            1950,
            new WeatherUnits("°F", "mph", "hPa"),
            new CurrentWeather("2026-10-14T16:00", 61.2, 59.8, 30, 805.1, 30.4, 7.5, 180, 2),
            [new HourlyWeather("2026-10-14T16:00", 61.2, 5, 2, 7.5, 180, 15.1)],
            [new DailyWeather("2026-10-14", 66.1, 40.3, 10, 3, 12.4)]
        );

    /// <summary>Trimmed example of an Open-Meteo forecast response body.</summary>
    public const string OpenMeteoForecastJson = """
        {
          "latitude": 39.09,
          "longitude": -104.87,
          "timezone": "America/Denver",
          "elevation": 1950.0,
          "current_units": { "temperature_2m": "°F", "wind_speed_10m": "mph", "surface_pressure": "hPa" },
          "current": {
            "time": "2026-10-14T16:00",
            "temperature_2m": 61.2,
            "apparent_temperature": 59.8,
            "relative_humidity_2m": 30,
            "surface_pressure": 805.1,
            "dew_point_2m": 30.4,
            "wind_speed_10m": 7.5,
            "wind_direction_10m": 180,
            "weather_code": 2
          },
          "hourly": {
            "time": ["2026-10-14T16:00", "2026-10-14T17:00"],
            "temperature_2m": [61.2, 60.1],
            "precipitation_probability": [5, null],
            "weather_code": [2, 3],
            "wind_speed_10m": [7.5, 8.0],
            "wind_direction_10m": [180, 190],
            "wind_gusts_10m": [15.1]
          },
          "daily": {
            "time": ["2026-10-14"],
            "temperature_2m_max": [66.1],
            "temperature_2m_min": [40.3],
            "precipitation_probability_max": [10],
            "weather_code": [3],
            "wind_speed_10m_max": [12.4]
          }
        }
        """;
}
