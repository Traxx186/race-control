using Microsoft.AspNetCore.Mvc;

namespace RaceControl.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View("./Index");
    }
}