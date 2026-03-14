using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("tbl_constituents")]
public class Constituent
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

    [Column("address")]
    public string? Address { get; set; }

    [Column("contact")]
    public string? Contact { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [NotMapped]
    public string FullName => $"{Firstname} {Middlename} {Lastname}".Trim();
}
