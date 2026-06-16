using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

[Table("crs_beneficiary_cache")]
public class CrsBeneficiaryCache
{
    [Key]
    [Column("beneficiary_cache_id")]
    public int BeneficiaryCacheId { get; set; }

    [Column("beneficiary_id")]
    [StringLength(45)]
    public string BeneficiaryId { get; set; } = string.Empty;

    [Column("full_name")]
    [StringLength(100)]
    public string? FullName { get; set; }

    [Column("first_name")]
    [StringLength(50)]
    public string? FirstName { get; set; }

    [Column("last_name")]
    [StringLength(50)]
    public string? LastName { get; set; }

    [Column("middle_name")]
    [StringLength(50)]
    public string? MiddleName { get; set; }

    [Column("sex")]
    [StringLength(10)]
    public string? Sex { get; set; }

    [Column("age")]
    public int? Age { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("marital_status")]
    [StringLength(20)]
    public string? MaritalStatus { get; set; }

    [Column("is_pwd")]
    public bool IsPwd { get; set; }

    [Column("is_senior")]
    public bool IsSenior { get; set; }

    [Column("cached_at")]
    public DateTime CachedAt { get; set; } = DateTime.Now;
}
