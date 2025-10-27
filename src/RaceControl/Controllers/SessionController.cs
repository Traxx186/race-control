using Microsoft.AspNetCore.Mvc;
using RaceControl.Database;
using RaceControl.Database.Entities;
using RaceControl.Services;

namespace RaceControl.Controllers;

public class SessionController(
    ILogger<HomeController> logger,
    CategoryService categoryService,
    RaceControlContext dbContext,
    WebsocketService websocketService)
    : ControllerBase
{
    [Route("/session")]
    [HttpGet]
    public IActionResult Index()
    {
        logger.LogInformation("[Session] Requesting active session and returning result");

        var currentSession = categoryService.ActiveSession;
        if (currentSession == null)
            return NotFound("No active session");

        return Ok(currentSession);
    }

    [Route("/session")]
    [HttpPatch]
    public async Task<IActionResult> UpdateSessionLatency([FromBody] PatchSessionLatency sessionLatency)
    {
        logger.LogInformation("[Session] Request to update session latency to {latency}", sessionLatency.Latency);

        var activeSession = categoryService.ActiveSession;
        if (activeSession == null)
            return NotFound("No active session to update");

        var category = dbContext.Categories.Single(c => c.Key == activeSession.CategoryKey);
        category.Latency = sessionLatency.Latency;

        logger.LogInformation("[Session] Saving changes to database");
        dbContext.ChangeTracker.DetectChanges();
        await dbContext.SaveChangesAsync();
        
        activeSession.Category = category;
        await websocketService.BroadcastEventAsync(MessageEvent.SessionChange, activeSession, CancellationToken.None);
        
        return Ok(category);
    }
}