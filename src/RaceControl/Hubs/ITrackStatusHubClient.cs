using RaceControl.Track;

namespace RaceControl.Hubs;

public interface ITrackStatusHubClient
{ 
    Task FlagChange(FlagData flagData);
}