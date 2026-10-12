using System.Text.Json.Serialization;

namespace RaceControl.Database.Entities;

public class Event
{
    /// <summary>
    /// Unique identifier of an Event.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Official name of the Event.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Meeting key of the Event.
    /// </summary>
    public string MeetingKey { get; set; }

    /// <summary>
    /// Location key of the Event.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Scheduled start time of the Event.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Scheduled start time of the Event.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// The identifier of the parent championship.
    /// </summary>
    public string ChampionshipId { get; set; }

    /// <summary>
    /// The parent <see cref="Championship"/>.
    /// </summary>
    public Championship Championship { get; set; }

    /// <summary>
    /// The related <see cref="Session"/> of the event.
    /// </summary>
    [JsonIgnore]
    public ICollection<Session> Sessions { get; } = new List<Session>();

}