using System.ComponentModel.DataAnnotations.Schema;

namespace GoodGovernanceApp.Models;

/// <summary>
/// POCO (not EF-mapped) — represents a beneficiary record from the CRS database.
/// CRS is READ ONLY for GGMS. Never write to CRS.
/// Table: val_beneficiaries | Primary key: beneficiary_id
/// </summary>
/// 

[Table("val_beneficiaries")]

public class Beneficiary
{
    // ── Primary Key ────────────────────────────────────────────────────────────
    public long Id { get; set; }
    public string BeneficiaryId { get; set; } = string.Empty;

    // ── Foreign Keys ───────────────────────────────────────────────────────────
    public long ResidentsId { get; set; }              // NOT NULL in DB
    public int? UserId { get; set; }                   // Nullable
    public string? CivilRegistryId { get; set; }       // VARCHAR in DB

    // ── Name Fields ────────────────────────────────────────────────────────────
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string? FullName { get; set; }

    // ── Personal Information ───────────────────────────────────────────────────
    public string? Sex { get; set; }

    /// <summary>
    /// Stored as VARCHAR in DB → keep raw + parsed version
    /// </summary>
    public string? DateOfBirthRaw { get; set; }

    [NotMapped]
    public DateTime? DateOfBirth
    {
        get
        {
            if (DateTime.TryParse(DateOfBirthRaw, out var parsed))
                return parsed;
            return null;
        }
    }

    /// <summary>
    /// Stored as VARCHAR in DB → keep raw + parsed version
    /// </summary>
    public string? AgeRaw { get; set; }

    [NotMapped]
    public int? Age
    {
        get
        {
            if (int.TryParse(AgeRaw, out var parsed))
                return parsed;
            return null;
        }
    }

    public string? MaritalStatus { get; set; }
    public string? Address { get; set; }

    // ── PWD Information ────────────────────────────────────────────────────────
    public bool IsPwd { get; set; }
    public string? PwdIdNo { get; set; }
    public string? DisabilityType { get; set; }
    public string? CauseOfDisability { get; set; }

    // ── Senior Citizen Information ─────────────────────────────────────────────
    public bool IsSenior { get; set; }
    public string? SeniorIdNo { get; set; }

    // ── Timestamps ────────────────────────────────────────────────────────────
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Computed Properties ───────────────────────────────────────────────────

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(LastName) && !string.IsNullOrWhiteSpace(FirstName)
            ? $"{LastName}, {FirstName} {MiddleName}".Trim()
            : FullName ?? "(no name)";

    public string DisplayAge =>
        Age.HasValue ? $"{Age} years old" : "N/A";

    public string DisplayDateOfBirth =>
        DateOfBirth.HasValue ? DateOfBirth.Value.ToString("MMMM dd, yyyy") : "N/A";

    public string Classifications
    {
        get
        {
            var tags = new List<string>();
            if (IsPwd) tags.Add("PWD");
            if (IsSenior) tags.Add("Senior Citizen");
            return tags.Count > 0 ? string.Join(", ", tags) : "None";
        }
    }
}