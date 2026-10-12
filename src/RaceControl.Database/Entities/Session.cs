namespace RaceControl.Database.Entities;

public class Session
{
    /// <summary>
    /// Unique identifier of the session.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the session.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The session key.
    /// </summary>
    public int Key { get; set; }

    /// <summary>
    /// The session Type.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Start time of the session in UTC.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// If the session is canceled, is set manually.
    /// </summary>
    public bool Cancelled { get; set; }

    /// <summary>
    /// The identifier of the parent event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The parent <see cref="Event"/>.
    /// </summary>
    public Event Event { get; set; }

}