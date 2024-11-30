using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using RaceControl.Services;
using RaceControl.Track;

namespace RaceControl.Controllers;

public class HomeController(ILogger<HomeController> logger, WebsocketService websocketService, TrackStatus trackStatus)
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

        logger.LogInformation("[Race Control] New user connected, sending current active flag");
        await WebsocketService.SendFlag(webSocket, trackStatus.ActiveFlag, HttpContext.RequestAborted);

        while (!HttpContext.RequestAborted.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer, HttpContext.RequestAborted);
            if (result.MessageType != WebSocketMessageType.Close)
                continue;

            logger.LogInformation("[Race Control] User disconnected, removing from open connection list");
            websocketService.Connections.Remove(webSocket);
        }
    }
}