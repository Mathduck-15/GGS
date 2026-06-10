using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

/// <summary>
/// Local-only SQLite cache of CRS val_beneficiaries.
/// Refreshed in full from CRS Hostinger when online.
/// No SyncId needed — this table is never merged, always replaced.
/// </summary>
[Table("crs_beneficiary_cache")]
public class CrsBeneficiaryCache
{
    [Key]
    public int BeneficiaryCacheId { get; set; }

    public string? BeneficiaryId { get; set; }
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string? Sex { get; set; }
    public string? AgeRaw { get; set; }
    public string? Address { get; set; }
    public string? DateOfBirthRaw { get; set; }
    public string? MaritalStatus { get; set; }
    public bool IsPwd { get; set; }
    public bool IsSenior { get; set; }
    public string? PwdIdNo { get; set; }
    public string? SeniorIdNo { get; set; }
    public string? DisabilityType { get; set; }
    public string? CauseOfDisability { get; set; }
    public long ResidentsId { get; set; }

    /// <summary>UTC timestamp of when this row was cached.</summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}
