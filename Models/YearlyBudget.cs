using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

    public virtual ICollection<DepartmentAllocation> Allocations { get; set; } = new List<DepartmentAllocation>();
}
