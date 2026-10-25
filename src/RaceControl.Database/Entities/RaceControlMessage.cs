namespace RaceControl.Database.Entities;

public class RaceControlMessage
{
    /// <summary>
    /// Unique identifier of a Message.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID of the session related to the message.
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Timestamp when the message was sent.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Category of the .
    /// </summary>
    public MessageCategory Category { get; set; }

    /// <summary>
    /// The send message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Flag displayed on track.
    /// </summary>
    public string? Flag { get; set; }

    /// <summary>
    /// Scope of the flag.
    /// </summary>
    public MessageScope? Scope { get; set; }

    /// <summary>
    /// The driver racing number associated to the message.
    /// </summary>
    public int? DriverNumber { get; set; }

    /// <summary>
    /// On which lap the message was sent, only used in race sessions.
    /// </summary>
    public int? Lap { get; set; }

    /// <summary>
    /// In which mini sector the send flag is displayed.
    /// </summary>
    public int? Sector { get; set; }

    /// <summary>
    /// The parent <see cref="Session"/>.
    /// </summary>
    public Session Session { get; set; }
}

/// <summary>
/// Possible scopes of a Race Control message
/// </summary>
public enum MessageScope
{
    Driver,
    Sector,
    Track,
}

/// <summary>
/// Possible categories of a Race Control message
/// </summary>
public enum MessageCategory
{
    CarEvent,
    Drs,
    Flag,
    Other,
    SafetyCar,
    SessionStatus
}