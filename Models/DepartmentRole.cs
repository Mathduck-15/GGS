using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

public class DepartmentRole
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }
}
