using System.Diagnostics.CodeAnalysis;

namespace Api.Services;

/// <summary>Bound from the <c>OpenMeteo</c> section of appsettings.json.</summary>
[ExcludeFromCodeCoverage ( Justification = "Simple DTO for configuration binding." )]
public sealed class OpenMeteoOptions
{
    public const string SectionName = "OpenMeteo";

    public string BaseUrl { get; set; } = "https://api.open-meteo.com";

    public int TimeoutSeconds { get; set; } = 10;
}
