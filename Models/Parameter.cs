using System.ComponentModel.DataAnnotations;

namespace GoodGovernanceApp.Models;

public class Parameter
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string? Description { get; set; }
}
