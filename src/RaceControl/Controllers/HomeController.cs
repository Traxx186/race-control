using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using RaceControl.Database.Entities;
using RaceControl.Services;
using RaceControl.Track;

namespace RaceControl.Controllers;

public class HomeController(ILogger<HomeController> logger, WebsocketService websocketService, TrackStatus trackStatus, CategoryService categoryService)
    : ControllerBase
{
    [Route("/")]
    public async Task Index()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        var buffer = new byte[4096];
        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        websocketService.Connections.Add(webSocket);
        
        logger.LogInformation("[Race Control] New user connected, sending current active flag and session");
        
        // Only send the current active session if one is present
        if (categoryService.ActiveSession != null)
        {
            var sessionMessage = new WebsocketMessage<Category>(MessageEvent.SessionChange, categoryService.ActiveSession.Category);
            await websocketService.SendAsync(webSocket, sessionMessage, HttpContext.RequestAborted); 
        }
        
        var flagMessage = new WebsocketMessage<FlagData>(MessageEvent.FlagChange, trackStatus.ActiveFlagData);
        await websocketService.SendAsync(webSocket, flagMessage, HttpContext.RequestAborted);

        while (!HttpContext.RequestAborted.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer, HttpContext.RequestAborted);
            if (result.MessageType != WebSocketMessageType.Close)
                continue;

            logger.LogInformation("[Race Control] User disconnected, removing from open connection list");
            websocketService.Connections.Remove(webSocket);
        }
    }

    [Route("/health")]
    public IActionResult Healthcheck()
    {
        logger.LogInformation("[Race Control] Healthcheck requested");
        return Ok("Ok");
    }
}