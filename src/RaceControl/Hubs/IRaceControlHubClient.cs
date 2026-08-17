using RaceControl.Data.Dtos;

namespace RaceControl.Hubs;

public interface IRaceControlHubClient
{
    Task FlagChange(FlagDataDto flagData);

    Task CategoryChange(CategoryDto categoryDto);
}