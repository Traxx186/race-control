using RaceControl.Data.Dtos;

namespace RaceControl.Hubs;

public interface ITrackStatusHubClient
{
    Task FlagChange(FlagDataDto flagData);
}