namespace RaceControl.Data.Dtos;

public sealed record SessionDto(
    string Description,
    string ShortName,
    string StartTime,
    string GmtOffset,
    string SessionType,
    int MeetingSessionKey,
    bool Cancelled = false
);