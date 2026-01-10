using RaceControl.Database.Entities;
using RaceControl.Track;

namespace RaceControl.Hubs;

public interface IRaceControlClient
{ 
    Task FlagChange(FlagData flagData);
    
    Task CategoryChange(Category category);
}