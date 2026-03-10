using System;
using System.ComponentModel.DataAnnotations;

namespace GoodGovernanceApp.Models;

public class SystemLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;
    
    public string? Details { get; set; }
}
