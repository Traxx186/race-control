using Microsoft.AspNetCore.Mvc;

namespace RaceControl.Controllers;

public class HealthController(ILogger<HealthController> logger) : ControllerBase
{

    [Route("/health")]
    public IActionResult Healthcheck()
    {
        logger.LogInformation("[Race Control] Healthcheck requested");
        return Ok("Ok");
    }
}