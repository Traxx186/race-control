namespace RaceControl.Data.Dtos.LiveTimingDtos;

/// <summary>
/// Structure of the content of a single RaceControlMessages message.
/// </summary>
public sealed class RaceControlMessageDto : ILiveTimingDto
{
    /// <summary>
    /// Timestamp when the message was sent in UTC.
    /// </summary>
    public DateTime Utc { get; set; }

    /// <summary>
    /// The driver racing number associated to the message.
    /// </summary>
    public string? RacingNumber { get; set; }

    /// <summary>
    /// On which lap the message was sent, only used in race sessions.
    /// </summary>
    public int? Lap { get; set; }

    /// <summary>
    /// The category of the message.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// The flag send from race control.
    /// </summary>
    public string? Flag { get; set; }

    /// <summary>
    /// What the scope is of the send flag
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// In which mini sector the send flag is displayed.
    /// </summary>
    public int? Sector { get; set; }

    /// <summary>
    /// The message send.
    /// </summary>
    public string Message { get; set; }
}