using RaceControl.Database.Entities;

namespace RaceControl.Hubs;

public interface ISessionHubClient
{ 
    Task CategoryChange(Category? category);
}