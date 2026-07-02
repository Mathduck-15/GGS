using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("officeallocations")]
public class OfficeAllocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int YearlyBudgetId { get; set; }

    [ForeignKey("YearlyBudgetId")]
    public virtual YearlyBudget? YearlyBudget { get; set; }

    /// <summary>The office_code string — kept for display/reporting purposes.</summary>
    [Column("office_code")]
    public string? OfficeCode { get; set; }

    /// <summary>FK to tbl_offices.id — used for the EF relationship.</summary>
    [Column("office_id")]
    public long? OfficeId { get; set; }

    [ForeignKey("OfficeId")]
    public virtual Office? Office { get; set; }

    [Required]
    public decimal AllocatedAmount { get; set; }

    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
