using System.Text.Json.Nodes;

namespace RaceControl.SignalR;

/// <summary>
/// SignalR message.
/// </summary>
public sealed record class Message(
    /// <summary>
    /// The hub name.
    /// </summary>
    string? H,
    /// <summary>
    /// The message method.
    /// </summary>
    string? M,
    /// <summary>
    /// The message's arguments.
    /// </summary>
    JsonArray? A
);