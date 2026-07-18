namespace GoodGovernanceApp.Models;

/// <summary>
/// Flat view model populated by an ADO.NET JOIN query against
/// tbl_transaction joined with tbl_offices and tbl_program_provision.
/// This is display-only — it is never persisted.
/// </summary>
public class TransactionRow
{
    public int    Id                     { get; set; }

    // ── Office / Program (from JOINs) ────────────────────────────────────────
    public string OfficeCode             { get; set; } = string.Empty;
    public string OfficeName             { get; set; } = string.Empty;
    public string ProgramName            { get; set; } = string.Empty;   // tbl_program_provision.program

    // ── tbl_transaction direct columns ───────────────────────────────────────
    public string VoucherCode            { get; set; } = string.Empty;
    public decimal Amount                { get; set; }
    public string TransactionType        { get; set; } = string.Empty;
    public string Status                 { get; set; } = string.Empty;
    public string Description            { get; set; } = string.Empty;
    public string Purpose                { get; set; } = string.Empty;
    public string RecipientType          { get; set; } = string.Empty;
    public string RecipientName          { get; set; } = string.Empty;
    public string Priority               { get; set; } = string.Empty;
    public string ReturnReason           { get; set; } = string.Empty;

    // ── Dates ─────────────────────────────────────────────────────────────────
    public DateTime? TransactionDate     { get; set; }
    public DateTime? DateApplied         { get; set; }
    public DateTime? DateApproved        { get; set; }
    public DateTime? ExpectedCompletion  { get; set; }
    public DateTime? ReturnedAt          { get; set; }
    public DateTime? CreatedAt           { get; set; }
    public DateTime? UpdatedAt           { get; set; }

    // ── IDs / FK values ───────────────────────────────────────────────────────
    public long?  UserId                 { get; set; }
    public long?  ConstituentId          { get; set; }
    public long?  RequestId              { get; set; }
    public long?  RegistryId             { get; set; }
    public long?  ServicesId             { get; set; }
    public long?  BudgetAllocationId     { get; set; }

    // ── Legacy / display helpers ──────────────────────────────────────────────
    /// <summary>Alias kept for any filter/search logic that still references it.</summary>
    public string ProjectCode            { get; set; } = string.Empty;
    public string ProjectName            { get; set; } = string.Empty;

    // ── Convenience display alias ─────────────────────────────────────────────
    public DateTime Date => TransactionDate ?? CreatedAt ?? DateTime.MinValue;
}
