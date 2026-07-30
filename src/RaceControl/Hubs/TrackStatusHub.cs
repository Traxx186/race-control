using Microsoft.AspNetCore.SignalR;
using RaceControl.Services;

namespace RaceControl.Hubs;

public class TrackStatusHub(ITrackStatusService trackStatusService) : Hub<ITrackStatusHubClient>
{
    public override Task OnConnectedAsync()
    {
        return Clients.Caller.FlagChange(trackStatusService.ActiveFlagData);
    }
}