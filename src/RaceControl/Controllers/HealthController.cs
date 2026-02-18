using Microsoft.AspNetCore.Mvc;

namespace RaceControl.Controllers;

public class HealthController(ILogger<HealthController> logger) : Controller
{
    public IActionResult Index()
    {
        logger.LogInformation("[Race Control] Healthcheck requested");
        return Ok("Ok");
    }
}