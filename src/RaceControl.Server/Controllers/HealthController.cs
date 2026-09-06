using Microsoft.AspNetCore.Mvc;

namespace RaceControl.Controllers;

public class HealthController : Controller
{
    public IActionResult Index()
    {
        return Ok("Ok");
    }
}