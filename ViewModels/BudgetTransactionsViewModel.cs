using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace GoodGovernanceApp.ViewModels;

public class BudgetTransactionsViewModel : ViewModelBase
{
    private readonly DatabaseHelper _db;

    // ── Raw data loaded from DB ───────────────────────────────────────────────
    private ObservableCollection<TransactionRow> _allRows = new();

    // ── Collection bound to the DataGrid (filtered view) ──────────────────────
    private ICollectionView _transactionsView = null!;

    // ── Filter fields ─────────────────────────────────────────────────────────
    private string _filterOfficeCode   = string.Empty;
    private string _filterProjectCode  = string.Empty;
    private string _filterStatus       = string.Empty;
    private string _filterFreeText     = string.Empty;

    // ── Selected row ──────────────────────────────────────────────────────────
    private TransactionRow? _selectedTransaction;

    // ── Loading indicator ─────────────────────────────────────────────────────
    private bool _isLoading;

    // =========================================================================
    // Public Properties
    // =========================================================================

    public ObservableCollection<TransactionRow> AllRows
    {
        get => _allRows;
        private set
        {
            _allRows = value;
            OnPropertyChanged();

            _transactionsView = CollectionViewSource.GetDefaultView(_allRows);
            _transactionsView.Filter = ApplyFilter;
            OnPropertyChanged(nameof(TransactionsView));
        }
    }

    public ICollectionView TransactionsView => _transactionsView;

    public TransactionRow? SelectedTransaction
    {
        get => _selectedTransaction;
        set { _selectedTransaction = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); }
    }

    // ── Filter properties (each setter triggers a view refresh) ───────────────
    public string FilterOfficeCode
    {
        get => _filterOfficeCode;
        set { _filterOfficeCode = value; OnPropertyChanged(); _transactionsView?.Refresh(); }
    }

    public string FilterProjectCode
    {
        get => _filterProjectCode;
        set { _filterProjectCode = value; OnPropertyChanged(); _transactionsView?.Refresh(); }
    }

    public string FilterStatus
    {
        get => _filterStatus;
        set { _filterStatus = value; OnPropertyChanged(); _transactionsView?.Refresh(); }
    }

    /// <summary>Free-text search across all visible columns.</summary>
    public string FilterFreeText
    {
        get => _filterFreeText;
        set { _filterFreeText = value; OnPropertyChanged(); _transactionsView?.Refresh(); }
    }

    // ── Status dropdown choices (empty string = no filter) ────────────────────
    public ObservableCollection<string> StatusOptions { get; } =
        new() { string.Empty, "Active", "Inactive", "Pending", "Completed" };

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand RefreshCommand    { get; }
    public ICommand ClearFilterCommand { get; }

    // =========================================================================
    // Constructor
    // =========================================================================

    public BudgetTransactionsViewModel()
    {
        _db = App.AppHost!.Services.GetRequiredService<DatabaseHelper>();

        // Initialise an empty view so bindings don't throw before data arrives
        _allRows = new ObservableCollection<TransactionRow>();
        _transactionsView = CollectionViewSource.GetDefaultView(_allRows);
        _transactionsView.Filter = ApplyFilter;

        RefreshCommand     = new RelayCommand(async _ => await LoadTransactionsAsync());
        ClearFilterCommand = new RelayCommand(_ => ClearFilters());

        _ = LoadTransactionsAsync();
    }

    // =========================================================================
    // Data Loading (ADO.NET)
    // =========================================================================

    private async Task LoadTransactionsAsync()
    {
        IsLoading = true;
        try
        {
            const string sql = @"
                SELECT
                    t.Id,
                    COALESCE(t.office_code, '')            AS OfficeCode,
                    COALESCE(t.project_code, '')           AS ProjectCode,
                    COALESCE(pd.project, '')               AS ProjectName,
                    COALESCE(pd.voucher_code, '')          AS VoucherCode,
                    t.Amount,
                    t.Date,
                    COALESCE(t.Description, '')            AS Description,
                    COALESCE(t.TransactionType, '')        AS TransactionType,
                    COALESCE(t.Status, '')                 AS Status,
                    t.CategoryId,
                    t.UserId
                FROM transactions t
                LEFT JOIN project_details pd
                       ON t.project_code = pd.project_details_id
                ORDER BY t.Date DESC;";

            DataTable dt = await _db.ExecuteQueryAsync(sql);

            var rows = new ObservableCollection<TransactionRow>();
            foreach (DataRow row in dt.Rows)
            {
                rows.Add(new TransactionRow
                {
                    Id              = Convert.ToInt32(row["Id"]),
                    OfficeCode      = row["OfficeCode"].ToString()  ?? string.Empty,
                    ProjectCode     = row["ProjectCode"].ToString() ?? string.Empty,
                    ProjectName     = row["ProjectName"].ToString() ?? string.Empty,
                    VoucherCode     = row["VoucherCode"].ToString() ?? string.Empty,
                    Amount          = Convert.ToDecimal(row["Amount"]),
                    Date            = row["Date"] == DBNull.Value
                                          ? DateTime.MinValue
                                          : Convert.ToDateTime(row["Date"]),
                    Description     = row["Description"].ToString()     ?? string.Empty,
                    TransactionType = row["TransactionType"].ToString() ?? string.Empty,
                    Status          = row["Status"].ToString()          ?? string.Empty,
                    CategoryId      = Convert.ToInt32(row["CategoryId"]),
                    UserId          = row["UserId"] == DBNull.Value
                                          ? null
                                          : Convert.ToInt64(row["UserId"])
                });
            }

            AllRows = rows;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BudgetTransactionsViewModel] Load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // =========================================================================
    // Filter Logic
    // =========================================================================

    private bool ApplyFilter(object obj)
    {
        if (obj is not TransactionRow row) return false;

        // Office code filter
        if (!string.IsNullOrWhiteSpace(FilterOfficeCode) &&
            !row.OfficeCode.Contains(FilterOfficeCode, StringComparison.OrdinalIgnoreCase))
            return false;

        // Project code / name filter
        if (!string.IsNullOrWhiteSpace(FilterProjectCode) &&
            !row.ProjectCode.Contains(FilterProjectCode, StringComparison.OrdinalIgnoreCase) &&
            !row.ProjectName.Contains(FilterProjectCode, StringComparison.OrdinalIgnoreCase))
            return false;

        // Status filter (exact match from dropdown)
        if (!string.IsNullOrWhiteSpace(FilterStatus) &&
            !row.Status.Equals(FilterStatus, StringComparison.OrdinalIgnoreCase))
            return false;

        // Free-text search across remaining visible columns
        if (!string.IsNullOrWhiteSpace(FilterFreeText))
        {
            string q = FilterFreeText;
            bool hit = row.OfficeCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.ProjectCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.ProjectName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.VoucherCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.TransactionType.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.Status.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.Amount.ToString().Contains(q, StringComparison.OrdinalIgnoreCase);
            if (!hit) return false;
        }

        return true;
    }

    private void ClearFilters()
    {
        FilterOfficeCode  = string.Empty;
        FilterProjectCode = string.Empty;
        FilterStatus      = string.Empty;
        FilterFreeText    = string.Empty;
    }
}
