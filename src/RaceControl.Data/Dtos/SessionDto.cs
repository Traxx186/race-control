using System.ComponentModel.DataAnnotations;

namespace RaceControl.Data.Dtos;

public sealed class SessionDto
{
    /// <summary>
    /// The identifier of the Event.
    /// </summary>
    [Key]
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
    /// The Session type.
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
    /// The parent <see cref="EventDto"/>.
    /// </summary>
    public EventDto Event { get; set; }
}