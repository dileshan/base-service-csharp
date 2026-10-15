using System.ComponentModel.DataAnnotations;

namespace Api.Models;

/// <summary>Query string for <c>GET /myweather</c>. Mirrors the Open-Meteo forecast request used by myweatherapp.</summary>
public sealed class MyWeatherRequest
{
    [Required]
    [Range(-90d, 90d)]
    public double? Latitude { get; set; }

    [Required]
    [Range(-180d, 180d)]
    public double? Longitude { get; set; }

    [Range(1, 16)]
    public int ForecastDays { get; set; } = 10;

    [AllowedValues("fahrenheit", "celsius")]
    public string TemperatureUnit { get; set; } = "fahrenheit";

    [AllowedValues("mph", "kmh", "ms", "kn")]
    public string WindSpeedUnit { get; set; } = "mph";
}
