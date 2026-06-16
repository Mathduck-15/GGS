using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("categories")]

public class Category
{
   

    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string? Description { get; set; }
    
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

}
