using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceControl.Database.Entities;

[Table("category")]
public class Category
{
    [MaxLength(64)]
    public required string Name { get; init; }
    
    public short Priority { get; init; }
    
    [Key]
    [MaxLength(32)]
    public required string Key { get; init; }

    public required ICollection<Session> Sessions { get; init; }
}