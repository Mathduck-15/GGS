using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("parameters")]
public class Parameter
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string? Description { get; set; }

    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

}
