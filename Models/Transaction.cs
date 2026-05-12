using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("transactions")]
public class Transaction
{
    [Key]
    public int Id { get; set; }

    [Column("project_code")]
    [StringLength(45)]
    public string? ProjectCode { get; set; }

    [Column("Amount")]
    public decimal? Amount { get; set; }

    [Column("voucher_code")]
    [StringLength(10)]
    public string? VoucherCode { get; set; }

    [Column("transaction_type")]
    [StringLength(45)]
    public string? TransactionType { get; set; } = "Expense";

    [Column("date")]
    public DateTime? Date { get; set; } = DateTime.Now;
}