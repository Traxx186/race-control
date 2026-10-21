using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceControl.Data.Dtos;

/// <summary>
/// A DTO representing relevant event data.
/// </summary>
public sealed class EventDto()
{
    /// <summary>
    /// The identifier of the Event.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The name of the Event.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The key of the Event.
    /// </summary>
    public string MeetingKey { get; set; }

    /// <summary>
    /// The location of the Event.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// The start date of the Event.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The end date of the Event.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// The time zone offset of the Event. Does not get returned in OData API calls.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GmtOffset { get; set; }

    /// <summary>
    /// The parent <see cref="Championship"/>.
    /// </summary>
    public ChampionshipDto Championship { get; set; }

    /// <summary>
    /// Sessions linked to the Event.
    /// </summary>
    public ICollection<SessionDto> Sessions { get; set; } = new List<SessionDto>();

}