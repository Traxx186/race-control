using Microsoft.AspNetCore.Mvc;
using RaceControl.Services;

namespace RaceControl.Controllers;

public class SessionController(ILogger<HomeController> logger, WebsocketService websocketService) 
    : ControllerBase
{
    
}