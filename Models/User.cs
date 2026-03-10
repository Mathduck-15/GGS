using System.ComponentModel.DataAnnotations;

namespace GoodGovernanceApp.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string Role { get; set; } = "User"; // SuperAdmin, Admin, Evaluator, User
    
    public bool IsActive { get; set; } = true;

    public int? DepartmentId { get; set; }
    public virtual Department? Department { get; set; }

    public int? DepartmentRoleId { get; set; }
    public virtual DepartmentRole? DepartmentRole { get; set; }
}
