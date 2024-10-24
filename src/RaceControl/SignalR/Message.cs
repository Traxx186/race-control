using System.Text.Json.Nodes;

namespace RaceControl.SignalR;

/// <summary>
/// SignalR message.
/// </summary>
public sealed record Message(
    string? H,
    string? M,
    JsonArray? A
);