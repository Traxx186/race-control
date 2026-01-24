using Microsoft.AspNetCore.SignalR;
using RaceControl.Services;

namespace RaceControl.Hubs;

public class SessionHub : Hub<ISessionHubClient>
{
    public async Task CurrentSession(CategoryService categoryService)
    {
        await Clients.Caller.CategoryChange(categoryService.ActiveSession?.Category);
    }
}