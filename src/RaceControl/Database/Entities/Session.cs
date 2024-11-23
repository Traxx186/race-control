using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceControl.Database.Entities;

[Table("session")]
public class Session 
{
    [Key]
    public Guid Id { get; init; }
    
    [MaxLength(64)]
    public required string Name { get; init; }
    
    [MaxLength(32)]
    public required string Key { get; init; }
    
    public DateTime Time { get; set; }

    [MaxLength(32)]
    public required string CategoryKey { get; init; }
    
    public required Category Category { get; init; }
}