using NodaTime;

namespace RaceControl.Database.Entities;

public class Session 
{
    public required string Id { get; init; }
    
    public required string Name { get; init; }
    
    public required string Key { get; init; }
    
    public LocalDateTime Time { get; set; }

    public required string CategoryKey { get; init; }
    
    public required Category Category { get; init; }
}