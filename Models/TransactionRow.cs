namespace GoodGovernanceApp.Models;

/// <summary>
/// Flat view model populated by an ADO.NET JOIN query between
/// <c>transactions</c> and <c>project_details</c>.
/// This is display-only — it is never persisted.
/// </summary>
public class TransactionRow
{
    public int    Id              { get; set; }
    public string OfficeCode      { get; set; } = string.Empty;
    public string ProjectCode     { get; set; } = string.Empty;   // project_details_id
    public string ProjectName     { get; set; } = string.Empty;   // project_details.project
    public string VoucherCode     { get; set; } = string.Empty;
    public decimal Amount         { get; set; }
    public DateTime Date          { get; set; }
    public string Description     { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Status          { get; set; } = string.Empty;
    public int    CategoryId      { get; set; }
    public long?  UserId          { get; set; }
}
