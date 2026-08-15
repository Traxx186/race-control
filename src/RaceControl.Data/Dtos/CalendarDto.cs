namespace RaceControl.Data.Dtos;

/// <summary>
/// The structure of the response from the calendar api.
/// </summary>
public sealed record CalendarDto(
    CalendarItemDto[] Races
);

/// <summary>
/// The structure of a calendar item of the calendar api.
/// </summary>
public sealed record CalendarItemDto(
    string Name,
    int Round,
    bool Canceled,
    Dictionary<string, DateTime> Sessions
);