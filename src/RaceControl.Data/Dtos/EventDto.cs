namespace RaceControl.Data.Dtos;

public sealed record EventDto(
    string MeetingName,
    string MeetingKey,
    bool IsTestEvent,
    string MeetingLocation,
    string MeetingStartDate,
    string MeetingEndDate,
    string GmtOffset
);