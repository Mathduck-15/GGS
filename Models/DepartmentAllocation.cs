using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

public class DepartmentAllocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column("YearlyBudgetId")]
    public long MasterBudgetId { get; set; }

    [ForeignKey("MasterBudgetId")]
    public virtual MasterBudget? MasterBudget { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

    [Required]
    public decimal AllocatedAmount { get; set; }


}
