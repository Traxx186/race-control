using Microsoft.AspNetCore.Mvc;

namespace RaceControl.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller
{
    public IActionResult Index()
    {
        logger.LogInformation("[Race Control] A user opened flag panel");
        return View("./Index");
    }
}