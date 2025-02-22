using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RaceControl.Track;

namespace RaceControl.Services;

public class WebsocketService(ILogger<WebsocketService> logger)
{
    public List<WebSocket> Connections { get; } = [];

    /// <summary>
    /// Sends a WebSocket message to all the connected clients.
    /// </summary>
    /// <param name="flagData">Flag data to send.</param>
    /// <param name="cancellationToken">The token that propagates the notification that the broadcasting should be cancelled.</param>
    public async Task BroadcastAsync(FlagData flagData, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Websocket Service] Sending flag '{flag}' to all connected clients", flagData.Flag);
        
        var openSockets = Connections.Where(x => x.State == WebSocketState.Open);
        foreach (var websocket in openSockets)
            await SendFlagAsync(websocket, flagData, cancellationToken);
    }
    
    /// <summary>
    /// Serializes <see cref="FlagData"/> to a JSON and sends the data to the connected client.
    /// </summary>
    /// <param name="websocket">Client to send to.</param>
    /// <param name="flagData">Flag data to be serialized.</param>
    /// <param name="cancellationToken">The token that propagates the notification that the broadcasting should be cancelled.</param>
    public static async Task SendFlagAsync(WebSocket websocket, FlagData flagData, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(flagData);
        var data = Encoding.UTF8.GetBytes(json);

        await websocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
    }
}