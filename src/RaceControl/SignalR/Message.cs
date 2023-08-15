using Newtonsoft.Json.Linq;

namespace RaceControl.SignalR;

/// <summary>
/// SignalR message.
/// </summary>
public struct Message
{
    /// <summary>
    /// The hub name.
    /// </summary>
    public string? H;

    /// <summary>
    /// The message method.
    /// </summary>
    public string? M;

    /// <summary>
    /// The message's arguments.
    /// </summary>
    public JArray? A;
}