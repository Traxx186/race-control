using System.Text.Json.Serialization;
using RaceControl.Data.Enums;

namespace RaceControl.Data.Dtos.Signalr;

/// <summary>
/// A DTO representing data relevant when a flag changed to be sent with SignalR.
/// </summary>
public sealed class FlagDataSignalrDto
{
    /// <summary>
    /// The current flag in a session.
    /// </summary>
    public Flag Flag { get; set; }

    /// <summary>
    /// The number of the driver for whom the flag is intended.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Driver { get; set; } = null;
}