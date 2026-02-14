using Microsoft.AspNetCore.SignalR;
using RaceControl.Services;

namespace RaceControl.Hubs;

public class SessionHub(CategoryService categoryService) : Hub<ISessionHubClient>
{
    public async Task CurrentSession(CategoryService service)
    {
        await Clients.Caller.CategoryChange(service.ActiveSession?.Category);
    }

    public override Task OnConnectedAsync()
    {
        return Clients.Caller.CategoryChange(categoryService.ActiveSession?.Category);
    }
}