using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("tbl_transaction")]
public class TblTransaction
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("program_id")]
    public long? ProgramId { get; set; }

    [ForeignKey("ProgramId")]
    public virtual ProgramProvision? Program { get; set; }

    [Column("budget_allocation_id")]
    public long? BudgetAllocationId { get; set; }

    [ForeignKey("BudgetAllocationId")]
    public virtual BudgetAllocation? BudgetAllocation { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("distributed_by_id")]
    public long? DistributedById { get; set; }

    [ForeignKey("DistributedById")]
    public virtual User? DistributedBy { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [Column("office_id")]
    public long? OfficeId { get; set; }

    [ForeignKey("OfficeId")]
    public virtual Office? Office { get; set; }

    [Column("services_id")]
    public long? ServicesId { get; set; }

    [ForeignKey("ServicesId")]
    public virtual TblService? Service { get; set; }

    [Column("transaction_date")]
    public DateTime? TransactionDate { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("voucher_code")]
    [StringLength(10)]
    public string? VoucherCode { get; set; }
}
