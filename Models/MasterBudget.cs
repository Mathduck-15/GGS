using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("master_budget")]
public class MasterBudget
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("budget_year")]
    public string FiscalYear { get; set; } = string.Empty;

    [Column("total_budget")]
    public decimal TotalAmount { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("allocated_budget")]
    public decimal AllocatedBudget { get; set; } = 0.00m;

    [Column("remaining_budget")]
    public decimal RemainingBudget { get; set; } = 0.00m;

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("created_by")]
    public long? CreatedById { get; set; }

    [ForeignKey("CreatedById")]
    public virtual User? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BudgetAllocation> Allocations { get; set; } = new List<BudgetAllocation>();

    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();
}
