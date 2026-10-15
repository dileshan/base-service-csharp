using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    public const string HealthyText = "healthy";

    [HttpGet]
    [Produces("text/plain")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public ContentResult Get() => Content(HealthyText, "text/plain");
}
