using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("tbl_employees")]
public class Employee
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("firstname")]
    public string? Firstname { get; set; }

    [Column("middlename")]
    public string? Middlename { get; set; }

    [Column("lastname")]
    public string? Lastname { get; set; }

    [Column("position_id")]
    public long? PositionId { get; set; }

    [ForeignKey("PositionId")]
    public virtual Position? Position { get; set; }

    [Column("designation_id")]
    public long? DesignationId { get; set; }

    [ForeignKey("DesignationId")]
    public virtual Designation? Designation { get; set; }

    [Column("office_id")]
    public long? OfficeId { get; set; }

    [ForeignKey("OfficeId")]
    public virtual Office? Office { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public string FullName => $"{Firstname} {Middlename} {Lastname}".Trim();
}
