using GoodGovernanceApp.Migrations;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;


[Table("transactions")]
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
    
    public long? UserId { get; set; }
    public User? User { get; set; }

    [Column("office_code")]
    [StringLength(20)]
    public string? OfficeCode { get; set; }

    [Column("project_code")]
    [StringLength(50)]
    public string? ProjectCode { get; set; }


    [Required]
    [StringLength(20)]
    public string TransactionType { get; set; } = "Expense"; // Income, Expense



    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Active";

    [Column("voucher_code")]
    [StringLength(10)]
    public string? VoucherCode { get; set; }
}
