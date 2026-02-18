using Microsoft.AspNetCore.SignalR;
using RaceControl.Track;

namespace RaceControl.Hubs;

public class TrackStatusHub(TrackStatus trackStatus) : Hub<ITrackStatusHubClient>
{
    public override Task OnConnectedAsync()
    {
        return Clients.Caller.FlagChange(trackStatus.ActiveFlagData);
    }
}