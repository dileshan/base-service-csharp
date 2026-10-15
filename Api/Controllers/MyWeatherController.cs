using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("myweather")]
[Produces("application/json")]
public sealed class MyWeatherController(IWeatherService weatherService) : ControllerBase
{
    /// <summary>Current conditions plus hourly and daily forecast for a location.</summary>
    [HttpGet]
    [ProducesResponseType<MyWeatherResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MyWeatherResponse>> Get(
        [FromQuery] MyWeatherRequest request,
        CancellationToken cancellationToken
    ) => Ok(await weatherService.GetForecastAsync(request, cancellationToken));
}
