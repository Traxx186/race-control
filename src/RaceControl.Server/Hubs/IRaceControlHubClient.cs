using RaceControl.Data.Dtos;

namespace RaceControl.Server.Hubs;

public interface IRaceControlHubClient
{
    Task FlagChange(FlagDataDto flagData);

    Task CategoryChange(ChampionshipDto championshipDto);
}