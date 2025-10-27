using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace RaceControl.Services;

public class WebsocketService(ILogger<WebsocketService> logger, IOptions<JsonOptions> jsonOptions)
{
    public List<WebSocket> Connections { get; } = [];

    /// <summary>
    /// Sends a WebSocket message to all the connected clients.
    /// </summary>
    /// <param name="messageEvent">The event of the message.</param>
    /// <param name="data">The content of the message.</param>
    /// <param name="cancellationToken">The token that propagates the notification that the broadcasting should be cancelled.</param>
    public async Task BroadcastEventAsync<T>(MessageEvent messageEvent, T data, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Websocket Service] Broadcast event {event} to all connected clients", messageEvent);
        
        var message = new WebsocketMessage<T>(messageEvent, data);
        var openSockets = Connections.Where(x => x.State == WebSocketState.Open);
        
        foreach (var websocket in openSockets)
            await SendAsync(websocket, message, cancellationToken);
    }
    
    /// <summary>
    /// Serializes <see cref="WebsocketMessage{T}"/> to a JSON and sends the data to given client.
    /// </summary>
    /// <param name="websocket">Client to send to.</param>
    /// <param name="websocketMessage">Message to be sent.</param>
    /// <param name="cancellationToken">The token that propagates the notification that the broadcasting should be cancelled.</param>
    public async Task SendAsync<T>(WebSocket websocket, WebsocketMessage<T> websocketMessage, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(websocketMessage, jsonOptions.Value.SerializerOptions);
        var data = Encoding.UTF8.GetBytes(json);

        await websocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
    }
}

/// <summary>
/// Structure of the websocket body
/// </summary>
/// <typeparam name="T">Payload data type</typeparam>
public record WebsocketMessage<T>(
    MessageEvent Event,
    T Data
);

/// <summary>
/// The supported event types that can be sent.
/// </summary>
[Flags]
public enum MessageEvent
{
    FlagChange,
    SessionChange
}