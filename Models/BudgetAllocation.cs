using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("officeallocations")]
public class BudgetAllocation
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Column("YearlyBudgetId")]
    public long? MasterBudgetId { get; set; }

    [ForeignKey("MasterBudgetId")]
    public virtual MasterBudget? MasterBudget { get; set; }

    [Column("office_id")]
    public long? OfficeId { get; set; }

    [ForeignKey("OfficeId")]
    public virtual Office? Office { get; set; }

    [Column("office_code")]
    public string? OfficeCode { get; set; }

    [Column("AllocatedAmount")]
    public decimal? AllocatedAmount { get; set; }

    [NotMapped]
    public decimal? RemainingAmount => (AllocatedAmount ?? 0m) - (UsedAmount ?? 0m);

    [Column("SpentAmount")]
    public decimal? UsedAmount { get; set; } = 0;

    [Column("updated_at")]
    public DateTime? updated_at { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();
}
