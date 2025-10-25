using NodaTime;

namespace RaceControl.Database.Entities;

public class Session 
{
    public string Id { get; set; }
    
    public string Name { get; set; }
    
    public string Key { get; set; }
    
    public LocalDateTime Time { get; set; }

    public string CategoryKey { get; set; }
    
    public Category Category { get; set; }
}