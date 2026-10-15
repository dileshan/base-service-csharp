using System.Text.Json.Serialization;

namespace Api.Models.OpenMeteo;

/// <summary>De-serialization model for the Open-Meteo <c>/v1/forecast</c> response.</summary>
public sealed class OpenMeteoForecastResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("elevation")]
    public double? Elevation { get; set; }

    [JsonPropertyName("current_units")]
    public Dictionary<string, string> CurrentUnits { get; set; } = [];

    [JsonPropertyName("current")]
    public OpenMeteoCurrent? Current { get; set; }

    [JsonPropertyName("hourly")]
    public OpenMeteoHourly? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public OpenMeteoDaily? Daily { get; set; }
}

public sealed class OpenMeteoCurrent
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public double? Temperature2m { get; set; }

    [JsonPropertyName("apparent_temperature")]
    public double? ApparentTemperature { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public double? RelativeHumidity2m { get; set; }

    [JsonPropertyName("surface_pressure")]
    public double? SurfacePressure { get; set; }

    [JsonPropertyName("dew_point_2m")]
    public double? DewPoint2m { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double? WindSpeed10m { get; set; }

    [JsonPropertyName("wind_direction_10m")]
    public double? WindDirection10m { get; set; }

    [JsonPropertyName("weather_code")]
    public int? WeatherCode { get; set; }
}

public sealed class OpenMeteoHourly
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m")]
    public List<double?> Temperature2m { get; set; } = [];

    [JsonPropertyName("precipitation_probability")]
    public List<double?> PrecipitationProbability { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = [];

    [JsonPropertyName("wind_speed_10m")]
    public List<double?> WindSpeed10m { get; set; } = [];

    [JsonPropertyName("wind_direction_10m")]
    public List<double?> WindDirection10m { get; set; } = [];

    [JsonPropertyName("wind_gusts_10m")]
    public List<double?> WindGusts10m { get; set; } = [];
}

public sealed class OpenMeteoDaily
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public List<double?> Temperature2mMax { get; set; } = [];

    [JsonPropertyName("temperature_2m_min")]
    public List<double?> Temperature2mMin { get; set; } = [];

    [JsonPropertyName("precipitation_probability_max")]
    public List<double?> PrecipitationProbabilityMax { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = [];

    [JsonPropertyName("wind_speed_10m_max")]
    public List<double?> WindSpeed10mMax { get; set; } = [];
}

/// <summary>Body Open-Meteo returns with a 400, e.g. <c>{"error": true, "reason": "Latitude must be in range of -90 to 90°."}</c>.</summary>
public sealed class OpenMeteoError
{
    [JsonPropertyName("error")]
    public bool Error { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
