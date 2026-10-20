namespace RaceControl.Data.Dtos;

/// <summary>
/// A DTO representing relevant event data.
/// </summary>
public class EventDto()
{
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
    public string StartDate { get; set; }

    /// <summary>
    /// The end date of the Event.
    /// </summary>
    public string EndDate { get; set; }

    /// <summary>
    /// The time zone offset of the Event.
    /// </summary>
    public string? GmtOffset { get; set; }

    /// <summary>
    /// Sessions linked to the Event.
    /// </summary>
    public ICollection<SessionDto> Sessions { get; set; } = new List<SessionDto>();

}