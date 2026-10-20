namespace RaceControl.Database.Entities;

public class Session
{
    /// <summary>
    /// Unique identifier of the session.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the Session.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The Session key.
    /// </summary>
    public int Key { get; set; }

    /// <summary>
    /// The Session Type.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Start time of the Session in UTC.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// If the Session is canceled, is set manually.
    /// </summary>
    public bool Cancelled { get; set; }

    /// <summary>
    /// The identifier of the parent event of the Session.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The parent <see cref="Event"/>.
    /// </summary>
    public Event Event { get; set; }

}