using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos;
using RaceControl.Server.Services;

namespace RaceControl.Server.Hubs;

public class RaceControlHub(
    ITrackStatusService trackStatusService,
    ICategoryService categoryService) : Hub<IRaceControlHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var category = categoryService.ActiveSession?.Category;
        var flagDataDto = new FlagDataDto(trackStatusService.ActiveFlag);

        if (category != null)
        {
            var categoryDto = new CategoryDto(category.Key, category.Latency);
            await Clients.Caller.CategoryChange(categoryDto);
            await Task.Delay(category.Latency * 1000);
        }

        await Clients.Caller.FlagChange(flagDataDto);
    }

    public void SendFlag(FlagDataDto flag)
    {
        categoryService.FlagQueue.Add(DateTime.UtcNow, flag);
    }
}