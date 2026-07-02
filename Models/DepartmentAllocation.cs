using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

public class DepartmentAllocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int YearlyBudgetId { get; set; }

    [ForeignKey("YearlyBudgetId")]
    public virtual YearlyBudget? YearlyBudget { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

    [Required]
    public decimal AllocatedAmount { get; set; }


}
