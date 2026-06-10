using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("officeallocations")]
public class OfficeAllocation : ISyncable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int YearlyBudgetId { get; set; }

    [ForeignKey("YearlyBudgetId")]
    public virtual YearlyBudget? YearlyBudget { get; set; }

    [Required]
    [Column("office_code")]
    public string OfficeCode { get; set; } = string.Empty;

    [ForeignKey("OfficeCode")]
    public virtual Office? Office { get; set; }

    [Required]
    public decimal AllocatedAmount { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Stable cross-PC identity used by SyncService.</summary>
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();
}
