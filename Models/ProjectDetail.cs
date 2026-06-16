using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("project_details")]
public class ProjectDetail
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("project_details_id")]
    public string? ProjectDetailsID { get; set; }

    [Column("project")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("office_code")]
    public string? OfficeCode { get; set; }

    [Column("total_budget")]
    public decimal? Budget { get; set; }

    [Column("contact_person")]
    public string? ContactPerson { get; set; }

    [Column("yearly_budget_id")]
    public int? YearlyBudgetId { get; set; }

    [Column("create_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("voucher_code")]
    [StringLength(10)]
    public string? VoucherCode { get; set; }

    [NotMapped]
    public decimal Spent { get; set; }

    [NotMapped]
    public decimal Remaining => (Budget ?? 0) - Spent;
    [Column("SyncId")]
    public Guid SyncId { get; set; } = Guid.NewGuid();

}