using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RaceControl.Database;
using RaceControl.Database.Entities;
using RaceControl.Hubs;
using RaceControl.Services;

namespace RaceControl.Controllers;

public class SessionController(
    ILogger<HealthController> logger,
    IHubContext<SessionHub, ISessionHubClient> sessionHubContext,
    CategoryService categoryService,
    RaceControlContext dbContext)
    : ControllerBase
{

    [Route("/api/session")]
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
        await sessionHubContext.Clients.All.CategoryChange(activeSession.Category);
        
        return Ok(category);
    }
}