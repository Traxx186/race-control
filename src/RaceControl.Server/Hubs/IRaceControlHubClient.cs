using RaceControl.Data.Dtos.Signalr;

namespace RaceControl.Server.Hubs;

public interface IRaceControlHubClient
{
    Task FlagChange(FlagDataSignalrDto flagData);

    Task ChampionshipChange(ChampionshipSignalrDto championshipDto);
}