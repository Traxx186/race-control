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
    /// Category of the message.
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

    /// <summary>
    /// Parses a string to a <see cref="MessageCategory"/>.
    /// </summary>
    /// <param name="category">String to parse.</param>
    /// <returns>The parsed <see cref="MessageCategory"/>.</returns>
    public static MessageCategory ParseCategory(string category)
    {
        return category.ToLower() switch
        {
            "carevent" => MessageCategory.CarEvent,
            "drs" => MessageCategory.Drs,
            "flag" => MessageCategory.Flag,
            "safetycar" => MessageCategory.SafetyCar,
            "sessionstatus" => MessageCategory.SessionStatus,
            _ => MessageCategory.Other
        };
    }

    /// <summary>
    /// Parses a string to a <see cref="MessageScope"/>.
    /// </summary>
    /// <param name="scope">String to parse.</param>
    /// <returns>The parsed <see cref="MessageScope"/>.</returns>
    public static MessageScope ParseScope(string scope)
    {
        return scope.ToLower() switch
        {
            "driver" => MessageScope.Driver,
            "sector" => MessageScope.Sector,
            "track" => MessageScope.Track,
            _ => throw new ArgumentException("Invalid scope value", nameof(scope))
        };
    }
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