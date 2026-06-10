using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("budget_allocations")]
public class BudgetAllocation : ISyncable
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("master_budget_id")]
    public long? MasterBudgetId { get; set; }

    [ForeignKey("MasterBudgetId")]
    public virtual MasterBudget? MasterBudget { get; set; }

    [Column("office_id")]
    public long? OfficeId { get; set; }

    [ForeignKey("OfficeId")]
    public virtual Office? Office { get; set; }

    [Column("allocated_amount")]
    public decimal AllocatedAmount { get; set; }



    [Column("allocated_by_id")]
    public long? AllocatedById { get; set; }

    [ForeignKey("AllocatedById")]
    public virtual User? AllocatedBy { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Stable cross-PC identity used by SyncService.</summary>
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();
}
