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
    private readonly AppDbContext _dbContext;

    private ObservableCollection<ConsolidatedTransactionsViewModel> _consolidatedRows = new();
    private ICollectionView _consolidatedTransactionsView = null!;

    public ObservableCollection<ConsolidatedTransactionsViewModel> ConsolidatedRows
    {
        get => _consolidatedRows;
        private set 
        { 
            _consolidatedRows = value; 
            OnPropertyChanged(); 
            _consolidatedTransactionsView = CollectionViewSource.GetDefaultView(_consolidatedRows);
            _consolidatedTransactionsView.Filter = ApplyConsolidatedFilter;
            OnPropertyChanged(nameof(ConsolidatedTransactionsView));
        }
    }

    public ICollectionView ConsolidatedTransactionsView => _consolidatedTransactionsView;


    // ── Raw data loaded from DB ───────────────────────────────────────────────
    private ObservableCollection<TransactionRow> _allRows = new();

    // ── Collection bound to the DataGrid (filtered view) ──────────────────────
    private ICollectionView _transactionsView = null!;

    // ── Tab 1 filter fields ───────────────────────────────────────────────────
    private string _filterOfficeCode   = string.Empty;
    private string _filterProjectCode  = string.Empty;
    private string _filterStatus       = string.Empty;
    private string _filterFreeText     = string.Empty;

    // ── Tab 2 (Consolidated) filter fields ────────────────────────────────────
    private string _consolidatedFilterFreeText     = string.Empty;
    private string _consolidatedFilterName         = string.Empty;
    private string _consolidatedFilterCivilId      = string.Empty;
    private string _consolidatedFilterTransactionType = string.Empty;
    private string _consolidatedFilterStatus       = string.Empty;

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
        set { _filterOfficeCode = value; OnPropertyChanged(); _transactionsView?.Refresh(); _consolidatedTransactionsView?.Refresh(); }
    }

    public ICommand PrintVoucherCommand { get; }

    public string FilterProjectCode
    {
        get => _filterProjectCode;
        set { _filterProjectCode = value; OnPropertyChanged(); _transactionsView?.Refresh(); _consolidatedTransactionsView?.Refresh(); }
    }

    public string FilterStatus
    {
        get => _filterStatus;
        set { _filterStatus = value; OnPropertyChanged(); _transactionsView?.Refresh(); _consolidatedTransactionsView?.Refresh(); }
    }

    /// <summary>Free-text search across all visible columns.</summary>
    public string FilterFreeText
    {
        get => _filterFreeText;
        set { _filterFreeText = value; OnPropertyChanged(); _transactionsView?.Refresh(); _consolidatedTransactionsView?.Refresh(); }
    }

    // ── Status dropdown choices (empty string = no filter) ────────────────────
    public ObservableCollection<string> StatusOptions { get; } =
        new() { string.Empty, "Active", "Inactive", "Pending", "Completed" };

    // ── Consolidated filter properties ────────────────────────────────────────
    public string ConsolidatedFilterFreeText
    {
        get => _consolidatedFilterFreeText;
        set { _consolidatedFilterFreeText = value; OnPropertyChanged(); _consolidatedTransactionsView?.Refresh(); }
    }

    public string ConsolidatedFilterName
    {
        get => _consolidatedFilterName;
        set { _consolidatedFilterName = value; OnPropertyChanged(); _consolidatedTransactionsView?.Refresh(); }
    }

    public string ConsolidatedFilterCivilId
    {
        get => _consolidatedFilterCivilId;
        set { _consolidatedFilterCivilId = value; OnPropertyChanged(); _consolidatedTransactionsView?.Refresh(); }
    }

    public string ConsolidatedFilterTransactionType
    {
        get => _consolidatedFilterTransactionType;
        set { _consolidatedFilterTransactionType = value; OnPropertyChanged(); _consolidatedTransactionsView?.Refresh(); }
    }

    public string ConsolidatedFilterStatus
    {
        get => _consolidatedFilterStatus;
        set { _consolidatedFilterStatus = value; OnPropertyChanged(); _consolidatedTransactionsView?.Refresh(); }
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand RefreshCommand             { get; }
    public ICommand ClearFilterCommand         { get; }
    public ICommand ClearConsolidatedFilterCommand { get; }

    // =========================================================================
    // Constructor
    // =========================================================================

    private string _currentDatabaseMode = string.Empty;


    public BudgetTransactionsViewModel()
    {
        if (App.AppHost == null) return;

        _db = App.AppHost.Services.GetRequiredService<DatabaseHelper>();
        _dbContext = App.AppHost.Services.GetRequiredService<AppDbContext>();

        // Initialise an empty view so bindings don't throw before data arrives
        _allRows = new ObservableCollection<TransactionRow>();
        _transactionsView = CollectionViewSource.GetDefaultView(_allRows);
        _transactionsView.Filter = ApplyFilter;

        _consolidatedRows = new ObservableCollection<ConsolidatedTransactionsViewModel>();
        _consolidatedTransactionsView = CollectionViewSource.GetDefaultView(_consolidatedRows);
        _consolidatedTransactionsView.Filter = ApplyConsolidatedFilter;

        RefreshCommand                 = new RelayCommand(async _ => await LoadTransactionsAsync());
        ClearFilterCommand             = new RelayCommand(_ => ClearFilters());
        ClearConsolidatedFilterCommand = new RelayCommand(_ => ClearConsolidatedFilters());


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
    // Data Loading
    // =========================================================================

    private async Task LoadConsolidatedTransactionsAsync()
    {
        IsLoading = true;

        try
        {
            var rowsList = await ConsolidatedTransactionsViewModel.GetTransactionsAsync(_dbContext);
            ConsolidatedRows = new ObservableCollection<ConsolidatedTransactionsViewModel>(rowsList);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[BudgetTransactionsViewModel] Consolidated load error: {ex.Message}");
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
            // Only columns that actually exist in the transactions table
            const string sql = @"
        SELECT
            t.Id,
            COALESCE(t.project_code, '')      AS ProjectCode,
            COALESCE(pd.project, '')          AS ProjectName,
            COALESCE(t.voucher_code, '')      AS VoucherCode,
            COALESCE(t.transaction_type, '')  AS TransactionType,
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
                    Id              = Convert.ToInt32(row["Id"]),
                    ProjectCode     = row["ProjectCode"].ToString()     ?? string.Empty,
                    ProjectName     = row["ProjectName"].ToString()     ?? string.Empty,
                    VoucherCode     = row["VoucherCode"].ToString()     ?? string.Empty,
                    TransactionType = row["TransactionType"].ToString() ?? string.Empty,
                    Amount          = row["Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Amount"]),
                    Date            = row["Date"] == DBNull.Value
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

        // Free-text search across all visible columns
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
                    || row.Amount.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.Date.ToString("yyyy-MM-dd").Contains(q, StringComparison.OrdinalIgnoreCase);
            if (!hit) return false;
        }

        return true;
    }

    private bool ApplyConsolidatedFilter(object obj)
    {
        if (obj is not ConsolidatedTransactionsViewModel row) return false;

        // Full name filter
        if (!string.IsNullOrWhiteSpace(ConsolidatedFilterName) &&
            !row.FullName.Contains(ConsolidatedFilterName, StringComparison.OrdinalIgnoreCase) &&
            !row.FirstName.Contains(ConsolidatedFilterName, StringComparison.OrdinalIgnoreCase) &&
            !row.LastName.Contains(ConsolidatedFilterName, StringComparison.OrdinalIgnoreCase))
            return false;

        // Civil registry ID filter
        if (!string.IsNullOrWhiteSpace(ConsolidatedFilterCivilId) &&
            !row.CivilRegistryId.Contains(ConsolidatedFilterCivilId, StringComparison.OrdinalIgnoreCase))
            return false;

        // Transaction type filter
        if (!string.IsNullOrWhiteSpace(ConsolidatedFilterTransactionType) &&
            !row.TransactionType.Contains(ConsolidatedFilterTransactionType, StringComparison.OrdinalIgnoreCase))
            return false;

        // Status filter
        if (!string.IsNullOrWhiteSpace(ConsolidatedFilterStatus) &&
            !row.Status.Equals(ConsolidatedFilterStatus, StringComparison.OrdinalIgnoreCase))
            return false;

        // Free-text search across all key columns
        if (!string.IsNullOrWhiteSpace(ConsolidatedFilterFreeText))
        {
            string q = ConsolidatedFilterFreeText;
            bool hit = row.BeneficiaryId.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.CivilRegistryId.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.FullName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.FirstName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.MiddleName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.LastName.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.ProjectCode.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || row.OfficeName.Contains(q, StringComparison.OrdinalIgnoreCase)
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

    private void ClearConsolidatedFilters()
    {
        ConsolidatedFilterFreeText        = string.Empty;
        ConsolidatedFilterName            = string.Empty;
        ConsolidatedFilterCivilId         = string.Empty;
        ConsolidatedFilterTransactionType = string.Empty;
        ConsolidatedFilterStatus          = string.Empty;
    }


}
