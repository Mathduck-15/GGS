using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GoodGovernanceApp.Models;

public class Department
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<DepartmentRole> Roles { get; set; } = new List<DepartmentRole>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
