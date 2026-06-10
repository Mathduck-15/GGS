using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

public class YearlyBudget : ISyncable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Stable cross-PC identity used by SyncService.</summary>
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();

    public virtual ICollection<OfficeAllocation> Allocations { get; set; } = new List<OfficeAllocation>();
}
