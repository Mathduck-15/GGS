using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("budget_allocations")]
public class BudgetAllocation
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

    [Column("amount")]
    public decimal AllocatedAmount { get; set; }

    [Column("office_type")]
    public string OfficeType { get; set; } = "service";

    [Column("program")]
    public string? Program { get; set; }

    [Column("remaining_amount")]
    public decimal? RemainingAmount { get; set; }

    [Column("used_amount")]
    public decimal UsedAmount { get; set; } = 0;

    [Column("status")]
    public string? Status { get; set; } = "active";



    [Column("allocated_by")]
    public long? AllocatedById { get; set; }

    [ForeignKey("AllocatedById")]
    public virtual User? AllocatedBy { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();

}
