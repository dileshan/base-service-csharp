namespace Api.Models;

/// <summary>Response for <c>GET /myweather</c>: current conditions plus hourly and daily forecasts.</summary>
public sealed record MyWeatherResponse(
    double Latitude,
    double Longitude,
    string Timezone,
    double? Elevation,
    WeatherUnits Units,
    CurrentWeather Current,
    IReadOnlyList<HourlyWeather> Hourly,
    IReadOnlyList<DailyWeather> Daily
);

public sealed record WeatherUnits(string Temperature, string WindSpeed, string Pressure);

public sealed record CurrentWeather(
    string Time,
    double? Temperature,
    double? ApparentTemperature,
    double? RelativeHumidity,
    double? SurfacePressure,
    double? DewPoint,
    double? WindSpeed,
    double? WindDirection,
    int? WeatherCode
);

public sealed record HourlyWeather(
    string Time,
    double? Temperature,
    double? PrecipitationProbability,
    int? WeatherCode,
    double? WindSpeed,
    double? WindDirection,
    double? WindGusts
);

public sealed record DailyWeather(
    string Date,
    double? TemperatureMax,
    double? TemperatureMin,
    double? PrecipitationProbabilityMax,
    int? WeatherCode,
    double? WindSpeedMax
);
