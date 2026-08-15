using Microsoft.AspNetCore.SignalR;
using RaceControl.Data.Dtos;
using RaceControl.Services;

namespace RaceControl.Hubs;

public class TrackStatusHub(ITrackStatusService trackStatusService) : Hub<ITrackStatusHubClient>
{
    public override Task OnConnectedAsync()
    {
        var dto = new FlagDataDto(trackStatusService.ActiveFlag);

        return Clients.Caller.FlagChange(dto);
    }
}