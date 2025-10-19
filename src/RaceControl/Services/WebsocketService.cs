using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RaceControl.Database.Entities;
using RaceControl.Track;

namespace RaceControl.Services;

public class WebsocketService(ILogger<WebsocketService> logger)
{
    public List<WebSocket> Connections { get; } = [];

    /// <summary>
    /// Sends a flag change WebSocket message to all the connected clients.
    /// </summary>
    /// <param name="flagData">Flag data to send.</param>
    /// <param name="cancellationToken">The token that propagates the notification that the broadcasting should be cancelled.</param>
    public async Task BroadcastFlagChangeAsync(FlagData flagData, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Websocket Service] Sending flag '{flag}' to all connected clients", flagData.Flag);
        
        var message = new Message<FlagData>("Flag", flagData);
        var openSockets = Connections.Where(x => x.State == WebSocketState.Open);
        
        foreach (var websocket in openSockets)
            await Broadcast(websocket, message, cancellationToken);
    }
    
    /// <summary>
    /// Sends an active category change WebSocket message to all the connected clients.
    /// </summary>
    /// <param name="category">Category to send.</param>
    /// <param name="cancellationToken">The token that propagates the notification that the broadcasting should be cancelled.</param>
    public async Task BroadcastCategoryChangeAsync(Category category, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Websocket Service] Sending new active category '{flag}' to all connected clients", category.Name);
        
        var message = new Message<Category>("Category", category);
        var openSockets = Connections.Where(x => x.State == WebSocketState.Open);
        
        foreach (var websocket in openSockets)
            await Broadcast(websocket, message, cancellationToken);
    }
    
    /// <summary>
    /// Serializes <see cref="Message{T}"/> to a JSON and sends the data to the connected client.
    /// </summary>
    /// <param name="websocket">Client to send to.</param>
    /// <param name="message">Message to be sent.</param>
    /// <param name="cancellationToken">The token that propagates the notification that the broadcasting should be cancelled.</param>
    private static async Task Broadcast<T>(WebSocket websocket, Message<T> message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);
        var data = Encoding.UTF8.GetBytes(json);

        await websocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
    }
    
    /// <summary>
    /// Sends a WebSocket message with the current flag the connected client.
    /// </summary>
    /// <param name="websocket">Client to send to.</param>
    /// <param name="flagData">Flag data to send.</param>
    /// <param name="cancellationToken">The token that propagates the notification that the broadcasting should be cancelled.</param>
    public static async Task SendFlagAsync(WebSocket websocket, FlagData flagData, CancellationToken cancellationToken)
    {
        var message = new Message<FlagData>("Flag", flagData);

        await Broadcast(websocket, message, cancellationToken);
    }
}

/// <summary>
/// Structure of the websocket body
/// </summary>
/// <typeparam name="T">Payload data type</typeparam>
public record Message<T>(
    string Type,
    T Data
);