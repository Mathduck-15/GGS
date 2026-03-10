using System;
using System.ComponentModel.DataAnnotations;

namespace GoodGovernanceApp.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public DateTime Date { get; set; } = DateTime.Now;
    
    [StringLength(255)]
    public string? Description { get; set; }
    
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    [Required]
    [StringLength(20)]
    public string TransactionType { get; set; } = "Expense"; // Income, Expense
}
