using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceControl.Database.Entities;

[Table("session")]
public class Session 
{
    [Key]
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Key { get; init; }
    public DateTime Time { get; set; }

    public required string CategoryKey { get; init; }
    public required Category Category { get; init; }
}