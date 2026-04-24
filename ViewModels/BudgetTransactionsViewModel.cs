using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Views;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace GoodGovernanceApp.ViewModels;

public class BudgetTransactionsViewModel : ViewModelBase
{
    private readonly DatabaseHelper _db;

    private ObservableCollection<ConsolidatedTransactionsViewModel> _consolidatedRows = new();
    public ObservableCollection<ConsolidatedTransactionsViewModel> ConsolidatedRows
    {
        get => _consolidatedRows;
        private set { _consolidatedRows = value; OnPropertyChanged(); }
    }


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

    public ICommand PrintVoucherCommand { get; }

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

    private string _currentDatabaseMode = string.Empty;


    public BudgetTransactionsViewModel()
    {
        if (App.AppHost == null) return;

        _db = App.AppHost.Services.GetRequiredService<DatabaseHelper>();

        // Initialise an empty view so bindings don't throw before data arrives
        _allRows = new ObservableCollection<TransactionRow>();
        _transactionsView = CollectionViewSource.GetDefaultView(_allRows);
        _transactionsView.Filter = ApplyFilter;

        RefreshCommand     = new RelayCommand(async _ => await LoadTransactionsAsync());
        ClearFilterCommand = new RelayCommand(_ => ClearFilters());


        PrintVoucherCommand = new RelayCommand(row =>
        {
            if (row is TransactionRow t)
            {
                var window = new VoucherPrintWindow(t);
                window.ShowDialog();
            }
        });

        _ = LoadConsolidatedTransactionsAsync();
        _ = LoadTransactionsAsync();
    }

    // =========================================================================
    // Data Loading (ADO.NET)
    // =========================================================================

    private async Task LoadConsolidatedTransactionsAsync()
    {
        IsLoading = true;

        try
        {
            const string sql = @"
        SELECT
            id, beneficiary_id, project_code, civil_registry_id,
            full_name, first_name, middle_name, last_name,
            office_id, office_name, transaction_type,
            amount, transaction_date, status, created_at
        FROM consolidated_transactions
        ORDER BY transaction_date DESC;";

            DataTable dt = await _db.ExecuteQueryAsync(sql);

            var rows = new ObservableCollection<ConsolidatedTransactionsViewModel>();

            foreach (DataRow row in dt.Rows)
            {
                rows.Add(new ConsolidatedTransactionsViewModel
                {
                    Id = Convert.ToInt32(row["id"]),
                    BeneficiaryId = row["beneficiary_id"].ToString() ?? string.Empty,
                    ProjectCode = row["project_code"].ToString() ?? string.Empty,
                    CivilRegistryId = row["civil_registry_id"].ToString() ?? string.Empty,
                    FullName = row["full_name"].ToString() ?? string.Empty,
                    FirstName = row["first_name"].ToString() ?? string.Empty,
                    MiddleName = row["middle_name"].ToString() ?? string.Empty,
                    LastName = row["last_name"].ToString() ?? string.Empty,
                    OfficeId = row["office_id"].ToString() ?? string.Empty,
                    OfficeName = row["office_name"].ToString() ?? string.Empty,
                    TransactionType = row["transaction_type"].ToString() ?? string.Empty,
                    Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]),
                    TransactionDate = row["transaction_date"] == DBNull.Value
                        ? DateOnly.MinValue
                        : DateOnly.FromDateTime(Convert.ToDateTime(row["transaction_date"])),
                    Status = row["status"].ToString() ?? string.Empty,
                    CreatedAt = row["created_at"] == DBNull.Value
                        ? DateTime.MinValue
                        : Convert.ToDateTime(row["created_at"])
                });
            }

            ConsolidatedRows = rows;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[BudgetTransactionsViewModel] Consolidated load error: {ex.Message}"
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTransactionsAsync()
    {
        IsLoading = true;

        try
        {
            const string sql = @"
        SELECT
            t.Id,
            COALESCE(t.project_code, '')     AS ProjectCode,
            COALESCE(pd.project, '')         AS ProjectName,
            COALESCE(t.voucher_code, '')     AS VoucherCode,
            t.Amount,
            t.Date
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
                    Id = Convert.ToInt32(row["Id"]),
                    ProjectCode = row["ProjectCode"].ToString() ?? string.Empty,
                    ProjectName = row["ProjectName"].ToString() ?? string.Empty,
                    VoucherCode = row["VoucherCode"].ToString() ?? string.Empty,
                    Amount = row["Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Amount"]),
                    Date = row["Date"] == DBNull.Value
                        ? DateTime.MinValue
                        : Convert.ToDateTime(row["Date"])
                });
            }

            AllRows = rows;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[BudgetTransactionsViewModel] Load error: {ex.Message}"
            );
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
            bool hit = row.ProjectCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.VoucherCode.Contains(q, StringComparison.OrdinalIgnoreCase)
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
