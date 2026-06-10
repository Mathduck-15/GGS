using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MySqlConnector;

namespace GoodGovernanceApp.ViewModels;

public class AddProjectViewModel : ViewModelBase
{
    private readonly LocalDbContext _dbContext;

    // ── Backing fields ──────────────────────────────────────────────────────────
    private string _projectId       = string.Empty;
    private string _projectName     = string.Empty;
    private string _description     = string.Empty;
    private string _totalBudget     = string.Empty;
    private string _beneficiaryId   = string.Empty;
    private string _beneficiaryName  = string.Empty;
    private bool   _hasBeneficiary   = false;
    private bool   _showNotFound     = false;
    private int?   _selectedYear;
    private string _selectedOfficeCode = string.Empty;
    private int?   _resolvedYearlyBudgetId;
    private string _voucherCode = string.Empty;
    public string transaction_type = "Expense";
    // ── Public properties ────────────────────────────────────────────────────────
    public string ProjectId
    {
        get => _projectId;
        private set { _projectId = value; OnPropertyChanged(); }
    }

    public string VoucherCode
    {
        get => _voucherCode;
        private set { _voucherCode = value; OnPropertyChanged(); }
    }

    public string ProjectName
    {
        get => _projectName;
        set { _projectName = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public string TotalBudget
    {
        get => _totalBudget;
        set { _totalBudget = value; OnPropertyChanged(); }
    }

    public string BeneficiaryId
    {
        get => _beneficiaryId;
        set
        {
            _beneficiaryId = value;
            OnPropertyChanged();
            // Clear previous result when the ID is edited
            HasBeneficiary = false;
            ShowNotFound   = false;
            BeneficiaryName = string.Empty;
        }
    }

    public string BeneficiaryName
    {
        get => _beneficiaryName;
        private set { _beneficiaryName = value; OnPropertyChanged(); }
    }

    public bool HasBeneficiary
    {
        get => _hasBeneficiary;
        private set { _hasBeneficiary = value; OnPropertyChanged(); }
    }

    public bool ShowNotFound
    {
        get => _showNotFound;
        private set { _showNotFound = value; OnPropertyChanged(); }
    }

    public int? SelectedYear
    {
        get => _selectedYear;
        set
        {
            _selectedYear = value;
            OnPropertyChanged();
            _ = LoadOfficeCodes();
        }
    }

    public string SelectedOfficeCode
    {
        get => _selectedOfficeCode;
        set
        {
            _selectedOfficeCode = value;
            OnPropertyChanged();
            ResolveYearlyBudgetId();
        }
    }

    // ── Collections ──────────────────────────────────────────────────────────────
    public ObservableCollection<int>    AvailableYears        { get; } = new();
    public ObservableCollection<string> FilteredOfficeCodes   { get; } = new();

    // ── Commands ─────────────────────────────────────────────────────────────────
    public ICommand SaveCommand              { get; }
    public ICommand CancelCommand            { get; }
    public ICommand LookupBeneficiaryCommand { get; }

    // Raised when save succeeds – the Window subscribes to close itself
    public event EventHandler? SaveSucceeded;

    // ── Constructor ──────────────────────────────────────────────────────────────
    public AddProjectViewModel()
    {
        var scope = App.AppHost!.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<LocalDbContext>();

        SaveCommand              = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
        CancelCommand            = new RelayCommand(_ => { });
        LookupBeneficiaryCommand = new RelayCommand(
            async _ => await LookupBeneficiaryAsync(),
            _  => !string.IsNullOrWhiteSpace(BeneficiaryId));

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await Task.WhenAll(GenerateProjectIdAsync(), LoadYearsAsync(), GenerateUniqueVoucherCodeAsync());
    }

    private async Task GenerateUniqueVoucherCodeAsync()
    {
        bool isUnique = false;
        string code = string.Empty;
        
        while (!isUnique)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            code = new string(System.Linq.Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());

            try
            {
                int count = await _dbContext.ProjectDetails.CountAsync(p => p.VoucherCode == code);
                if (count == 0) isUnique = true;
            }
            catch 
            {
                isUnique = true; // Safety fallback
            }
        }
        
        VoucherCode = code;
        System.Windows.Application.Current?.Dispatcher.Invoke(
            System.Windows.Input.CommandManager.InvalidateRequerySuggested);
    }

    // ── Project-ID generation ────────────────────────────────────────────────────
    private async Task GenerateProjectIdAsync()
    {
        try
        {
            var last = await _dbContext.ProjectDetails
                .Where(p => p.ProjectDetailsID != null && p.ProjectDetailsID.StartsWith("OPP-"))
                .OrderByDescending(p => p.ProjectDetailsID)
                .Select(p => p.ProjectDetailsID)
                .FirstOrDefaultAsync();

            int nextSeq = 1;

            if (!string.IsNullOrEmpty(last))
            {
                // format: OPP-YYYY-NNNN
                var parts = last.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int n))
                    nextSeq = n + 1;
            }

            int year = DateTime.Now.Year;
            ProjectId = $"OPP-{year}-{nextSeq:D4}";
            System.Windows.Application.Current?.Dispatcher.Invoke(
                System.Windows.Input.CommandManager.InvalidateRequerySuggested);
        }
        catch
        {
            ProjectId = $"OPP-{DateTime.Now.Year}-0001";
            System.Windows.Application.Current?.Dispatcher.Invoke(
                System.Windows.Input.CommandManager.InvalidateRequerySuggested);
        }
    }

    // ── Load distinct years from YearlyBudgets ───────────────────────────────────
    private async Task LoadYearsAsync()
    {
        try
        {
            var years = await _dbContext.YearlyBudgets
                .Select(yb => yb.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            AvailableYears.Clear();
            foreach (var y in years)
                AvailableYears.Add(y);
        }
        catch { /* leave empty */ }
    }

    // ── Filter office codes by year ──────────────────────────────────────────────
    private async Task LoadOfficeCodes()
    {
        FilteredOfficeCodes.Clear();
        SelectedOfficeCode = string.Empty;
        _resolvedYearlyBudgetId = null;

        if (_selectedYear == null) return;

        try
        {
            var codes = await _dbContext.OfficeAllocations
                .Include(oa => oa.YearlyBudget)
                .Where(oa => oa.YearlyBudget.Year == _selectedYear.Value && !string.IsNullOrEmpty(oa.OfficeCode))
                .Select(oa => oa.OfficeCode)
                .OrderBy(c => c)
                .ToListAsync();

            foreach (var code in codes)
            {
                FilteredOfficeCodes.Add(code);
            }
        }
        catch { /* leave empty */ }
    }

    // ── Resolve YearlyBudgetId silently in the background ───────────────────────
    private void ResolveYearlyBudgetId()
    {
        _resolvedYearlyBudgetId = null;
        if (_selectedYear == null || string.IsNullOrEmpty(_selectedOfficeCode)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var id = await _dbContext.OfficeAllocations
                    .Include(oa => oa.YearlyBudget)
                    .Where(oa => oa.YearlyBudget.Year == _selectedYear!.Value && oa.OfficeCode == _selectedOfficeCode)
                    .Select(oa => oa.YearlyBudgetId)
                    .FirstOrDefaultAsync();

                if (id != 0)
                    _resolvedYearlyBudgetId = id;
            }
            catch { /* silently ignore */ }
        });
    }

    // ── Beneficiary lookup ───────────────────────────────────────────────────────
    private async Task LookupBeneficiaryAsync()
    {
        HasBeneficiary  = false;
        ShowNotFound    = false;
        BeneficiaryName = string.Empty;

        string id = BeneficiaryId.Trim();
        if (string.IsNullOrWhiteSpace(id)) return;

        const string sql = @"
            SELECT full_name
            FROM val_beneficiaries
            WHERE beneficiary_id = @id
            LIMIT 1;";
        try
        {
            using var conn = new MySqlConnection(GoodGovernanceApp.Data.DatabaseConfig.CrsConnectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var result = await cmd.ExecuteScalarAsync();
            string? name = result?.ToString();

            if (!string.IsNullOrWhiteSpace(name))
            {
                BeneficiaryName = name;
                HasBeneficiary  = true;
            }
            else
            {
                ShowNotFound = true;
            }
        }
        catch
        {
            ShowNotFound = true;
        }

        System.Windows.Application.Current?.Dispatcher.Invoke(
            System.Windows.Input.CommandManager.InvalidateRequerySuggested);
    }

    // ── Validation ───────────────────────────────────────────────────────────────
    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(ProjectId)
            && !string.IsNullOrWhiteSpace(VoucherCode)
            && !string.IsNullOrWhiteSpace(ProjectName)
            && !string.IsNullOrWhiteSpace(SelectedOfficeCode)
            && (string.IsNullOrWhiteSpace(TotalBudget) || decimal.TryParse(TotalBudget, out _));
    }

    // ── Save ─────────────────────────────────────────────────────────────────────
    private async Task SaveAsync()
    {
        if (!CanSave()) return;

        // Safety net: these should never be empty if CanSave() passed, but be explicit
        if (string.IsNullOrWhiteSpace(ProjectId) || string.IsNullOrWhiteSpace(VoucherCode))
        {
            MessageBox.Show("Project ID or Voucher Code is not yet generated. Please wait a moment and try again.",
                "Not Ready", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }


        decimal? budget = null;
        if (!string.IsNullOrWhiteSpace(TotalBudget) && decimal.TryParse(TotalBudget, out decimal parsed))
            budget = parsed;

        if (_resolvedYearlyBudgetId == null && _selectedYear.HasValue && !string.IsNullOrEmpty(_selectedOfficeCode))
        {
            var id = await _dbContext.OfficeAllocations
                .Include(oa => oa.YearlyBudget)
                .Where(oa => oa.YearlyBudget.Year == _selectedYear!.Value && oa.OfficeCode == _selectedOfficeCode)
                .Select(oa => oa.YearlyBudgetId)
                .FirstOrDefaultAsync();

            if (id != 0)
                _resolvedYearlyBudgetId = id;
        }

        try
        {
            var project = new ProjectDetail
            {
                ProjectDetailsID = ProjectId,
                Name = ProjectName,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                OfficeCode = SelectedOfficeCode,
                Budget = budget,
                ContactPerson = string.IsNullOrWhiteSpace(BeneficiaryId) ? null : BeneficiaryId.Trim(),
                YearlyBudgetId = _resolvedYearlyBudgetId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                VoucherCode = VoucherCode
            };
            _dbContext.ProjectDetails.Add(project);

            var transaction = new Transaction
            {
                ProjectCode = ProjectId,
                Amount = budget ?? 0,
                VoucherCode = VoucherCode,
                Date = DateTime.Now,
                TransactionType = transaction_type
            };
            _dbContext.Transactions.Add(transaction);

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save project or transaction:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        MessageBox.Show($"Project \"{ProjectName}\" saved successfully!", "Success",
            MessageBoxButton.OK, MessageBoxImage.Information);

        SaveSucceeded?.Invoke(this, EventArgs.Empty);
    }
}

