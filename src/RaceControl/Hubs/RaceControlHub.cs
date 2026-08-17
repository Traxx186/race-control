using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos;
using RaceControl.Services;

namespace RaceControl.Hubs;

public class RaceControlHub(
    ITrackStatusService trackStatusService,
    ICategoryService categoryService) : Hub<IRaceControlHubClient>
{
    public override Task OnConnectedAsync()
    {
        var category = categoryService.ActiveSession?.Category;
        var flagDataDto = new FlagDataDto(trackStatusService.ActiveFlag);
        var categoryDto = new CategoryDto(category?.Key ?? string.Empty, category?.Latency ?? 0);

        return Task.WhenAll(
            Clients.Caller.FlagChange(flagDataDto),
            Clients.Caller.CategoryChange(categoryDto)
        );
    }
}