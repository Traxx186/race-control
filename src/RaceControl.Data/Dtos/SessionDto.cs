namespace RaceControl.Data.Dtos;

public sealed record SessionDto(
    string Description,
    string StartTime,
    string GmtOffset,
    string SessionType,
    int MeetingSessionKey
);