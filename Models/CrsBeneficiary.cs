namespace GoodGovernanceApp.Models;

/// <summary>
/// POCO (not EF-mapped) — represents a beneficiary record from the CRS database.
/// CRS is READ ONLY for GGMS. Never write to CRS.
/// Table: val_beneficiaries | Primary key: beneficiary_id
/// </summary>
public class CrsBeneficiary
{
    public string BeneficiaryId { get; set; } = string.Empty;
    public string FirstName     { get; set; } = string.Empty;
    public string LastName      { get; set; } = string.Empty;
    public string MiddleName    { get; set; } = string.Empty;
    /// <summary>Address is currently empty in CRS. Display placeholder.</summary>
    public string Address       { get; set; } = "(address not yet available)";
    public string FullName      => $"{LastName}, {FirstName} {MiddleName}".Trim();
}
