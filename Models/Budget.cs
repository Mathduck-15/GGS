using System.ComponentModel.DataAnnotations;

namespace GoodGovernanceApp.Models;

public class Budget
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public int Year { get; set; }

    // Office linkage
    public long? OfficeId { get; set; }
    public virtual Office? Office { get; set; }
}
