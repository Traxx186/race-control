using Microsoft.AspNetCore.Mvc;

namespace RaceControl.Server.Controllers;

public class HealthController : Controller
{
    public IActionResult Index()
    {
        return Ok("Ok");
    }
}