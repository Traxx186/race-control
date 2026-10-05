namespace RaceControl.Data.Dtos;

public sealed record EventDto(
    string MeetingName,
    string MeetingKey,
    bool IsTestEvent
);