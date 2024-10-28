using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceControl.Database.Entities;

[Table("category")]
public class Category
{
    public required string Name { get; init; }
    public short Priority { get; init; }
    
    [Key]
    public required string Key { get; init; }

    public required List<Session> Sessions { get; init; }
}