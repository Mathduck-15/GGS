using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

public class YearlyBudget
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    public virtual ICollection<OfficeAllocation> Allocations { get; set; } = new List<OfficeAllocation>();

    [Column("SyncId")]
    public System.Guid SyncId { get; set; } = System.Guid.NewGuid();

    [Column("updated_at")]
    public System.DateTime? UpdatedAt { get; set; }
}
