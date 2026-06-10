using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace GoodGovernanceApp.ViewModels;

public class ReportsViewModel : ViewModelBase
{
    private readonly AppDbContext _context;

    // ── Report Type List ──────────────────────────────────────────────────────
    public ObservableCollection<string> ReportTypes { get; } = new()
    {
        "Financial Overview",
        "Consolidated Transactions Analytics",
        "CRS Beneficiary Analytics",
        "User Activity Log",
        "Budget Summary by Category",
        "Transaction History",
        "Parameters List",
        "Office Budget Allocation",
        "System Overview"
    };

    private string _selectedReportType = string.Empty;
    public string SelectedReportType
    {
        get => _selectedReportType;
        set
        {
            _selectedReportType = value;
            OnPropertyChanged();
            _ = GenerateReportAsync();
        }
    }

    // ── Visibility flags ──────────────────────────────────────────────────────
    public bool IsFinancialOverviewVisible          => SelectedReportType == "Financial Overview";
    public bool IsConsolidatedAnalyticsVisible      => SelectedReportType == "Consolidated Transactions Analytics";
    public bool IsCrsAnalyticsVisible               => SelectedReportType == "CRS Beneficiary Analytics";
    public bool IsUserActivityLogVisible            => SelectedReportType == "User Activity Log";
    public bool IsBudgetSummaryVisible              => SelectedReportType == "Budget Summary by Category";
    public bool IsTransactionHistoryVisible         => SelectedReportType == "Transaction History";
    public bool IsParametersListVisible             => SelectedReportType == "Parameters List";
    public bool IsDepartmentalBudgetVisible         => SelectedReportType == "Office Budget Allocation";
    public bool IsSystemOverviewVisible             => SelectedReportType == "System Overview";

    // ── Shared ────────────────────────────────────────────────────────────────
    public Func<double, string> CurrencyFormatter { get; } = v => v.ToString("C0");

    // ── Financial Overview (from DashboardViewModel) ─────────────────────────
    private decimal _totalBudget;
    public decimal TotalBudget { get => _totalBudget; set { _totalBudget = value; OnPropertyChanged(); } }

    private decimal _totalExpenses;
    public decimal TotalExpenses { get => _totalExpenses; set { _totalExpenses = value; OnPropertyChanged(); } }

    private int _activeUsers;
    public int ActiveUsers { get => _activeUsers; set { _activeUsers = value; OnPropertyChanged(); } }

    private int _totalProjects;
    public int TotalProjects { get => _totalProjects; set { _totalProjects = value; OnPropertyChanged(); } }

    private SeriesCollection _deptBudgetSeries = new();
    public SeriesCollection DeptBudgetSeries { get => _deptBudgetSeries; set { _deptBudgetSeries = value; OnPropertyChanged(); } }

    private SeriesCollection _projectBudgetSeries = new();
    public SeriesCollection ProjectBudgetSeries { get => _projectBudgetSeries; set { _projectBudgetSeries = value; OnPropertyChanged(); } }

    // ── Consolidated Analytics ────────────────────────────────────────────────
    private int _consolidatedTotalCount;
    public int ConsolidatedTotalCount { get => _consolidatedTotalCount; set { _consolidatedTotalCount = value; OnPropertyChanged(); } }

    private decimal _consolidatedTotalAmount;
    public decimal ConsolidatedTotalAmount { get => _consolidatedTotalAmount; set { _consolidatedTotalAmount = value; OnPropertyChanged(); } }

    private decimal _consolidatedAvgAmount;
    public decimal ConsolidatedAvgAmount { get => _consolidatedAvgAmount; set { _consolidatedAvgAmount = value; OnPropertyChanged(); } }

    private SeriesCollection _consolidatedTypeSeries = new();
    public SeriesCollection ConsolidatedTypeSeries { get => _consolidatedTypeSeries; set { _consolidatedTypeSeries = value; OnPropertyChanged(); } }

    private SeriesCollection _consolidatedMonthlySeries = new();
    public SeriesCollection ConsolidatedMonthlySeries { get => _consolidatedMonthlySeries; set { _consolidatedMonthlySeries = value; OnPropertyChanged(); } }

    private ObservableCollection<string> _consolidatedMonthlyLabels = new();
    public ObservableCollection<string> ConsolidatedMonthlyLabels { get => _consolidatedMonthlyLabels; set { _consolidatedMonthlyLabels = value; OnPropertyChanged(); } }

    // ── CRS Analytics ─────────────────────────────────────────────────────────
    private int _crsTotalCount;
    public int CrsTotalCount { get => _crsTotalCount; set { _crsTotalCount = value; OnPropertyChanged(); } }

    private int _crsPwdCount;
    public int CrsPwdCount { get => _crsPwdCount; set { _crsPwdCount = value; OnPropertyChanged(); } }

    private int _crsSeniorCount;
    public int CrsSeniorCount { get => _crsSeniorCount; set { _crsSeniorCount = value; OnPropertyChanged(); } }

    private SeriesCollection _crsGenderSeries = new();
    public SeriesCollection CrsGenderSeries { get => _crsGenderSeries; set { _crsGenderSeries = value; OnPropertyChanged(); } }

    private SeriesCollection _crsAgeGroupSeries = new();
    public SeriesCollection CrsAgeGroupSeries { get => _crsAgeGroupSeries; set { _crsAgeGroupSeries = value; OnPropertyChanged(); } }

    private ObservableCollection<string> _crsAgeGroupLabels = new()
    {
        "0–17", "18–35", "36–60", "60+"
    };
    public ObservableCollection<string> CrsAgeGroupLabels { get => _crsAgeGroupLabels; set { _crsAgeGroupLabels = value; OnPropertyChanged(); } }

    // ── Existing report collections ───────────────────────────────────────────
    private ObservableCollection<SystemLog>  _userActivityLogs    = new();
    public ObservableCollection<SystemLog>   UserActivityLogs     { get => _userActivityLogs;    set { _userActivityLogs    = value; OnPropertyChanged(); } }

    private ObservableCollection<object>     _budgetSummaries     = new();
    public ObservableCollection<object>      BudgetSummaries      { get => _budgetSummaries;     set { _budgetSummaries     = value; OnPropertyChanged(); } }

    private ObservableCollection<Transaction> _transactionHistory = new();
    public ObservableCollection<Transaction>  TransactionHistory  { get => _transactionHistory;  set { _transactionHistory  = value; OnPropertyChanged(); } }

    private ObservableCollection<Parameter> _parametersList       = new();
    public ObservableCollection<Parameter>  ParametersList        { get => _parametersList;       set { _parametersList       = value; OnPropertyChanged(); } }

    private ObservableCollection<object>    _departmentalBudgets  = new();
    public ObservableCollection<object>     DepartmentalBudgets   { get => _departmentalBudgets;  set { _departmentalBudgets  = value; OnPropertyChanged(); } }

    private ObservableCollection<object>    _systemOverview       = new();
    public ObservableCollection<object>     SystemOverview        { get => _systemOverview;       set { _systemOverview       = value; OnPropertyChanged(); } }

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand GenerateReportCommand { get; }
    public ICommand PrintExportCommand    { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public ReportsViewModel()
    {
        try { _context = App.AppHost!.Services.GetRequiredService<AppDbContext>(); }
        catch { }

        GenerateReportCommand = new RelayCommand(async _ => await GenerateReportAsync());
        PrintExportCommand    = new RelayCommand(_ => { /* TODO: PDF/Excel export */ });

        SelectedReportType = ReportTypes[0];
    }

    // ── Main dispatch ─────────────────────────────────────────────────────────
    private async Task GenerateReportAsync()
    {
        // Safety: DI container may already be disposed during app shutdown
        if (_context == null || string.IsNullOrEmpty(SelectedReportType)) return;
        if (System.Windows.Application.Current == null)                   return;

        // Refresh all visibility flags at once
        OnPropertyChanged(nameof(IsFinancialOverviewVisible));
        OnPropertyChanged(nameof(IsConsolidatedAnalyticsVisible));
        OnPropertyChanged(nameof(IsCrsAnalyticsVisible));
        OnPropertyChanged(nameof(IsUserActivityLogVisible));
        OnPropertyChanged(nameof(IsBudgetSummaryVisible));
        OnPropertyChanged(nameof(IsTransactionHistoryVisible));
        OnPropertyChanged(nameof(IsParametersListVisible));
        OnPropertyChanged(nameof(IsDepartmentalBudgetVisible));
        OnPropertyChanged(nameof(IsSystemOverviewVisible));

        try
        {
            switch (SelectedReportType)
            {
                case "Financial Overview":
                    await LoadFinancialOverviewAsync();
                    break;

                case "Consolidated Transactions Analytics":
                    await LoadConsolidatedAnalyticsAsync();
                    break;

                case "CRS Beneficiary Analytics":
                    await LoadCrsAnalyticsAsync();
                    break;

                case "User Activity Log":
                    var logs = await _context.SystemLogs
                        .Include(l => l.User)
                        .OrderByDescending(l => l.Timestamp)
                        .ToListAsync();
                    UserActivityLogs = new ObservableCollection<SystemLog>(logs);
                    break;

                case "Budget Summary by Category":
                    var categories = await _context.Categories
                        .Include(c => c.Budgets)
                        .ToListAsync();
                    var totalExpenses = await _context.Transactions
                        .Where(t => t.TransactionType == "Expense")
                        .SumAsync(t => t.Amount ?? 0);
                    var summaries = categories.Select(c => new
                    {
                        CategoryName     = c.Name,
                        TotalBudget      = c.Budgets.Sum(b => b.Amount),
                        TotalExpenses    = totalExpenses,
                        RemainingBalance = c.Budgets.Sum(b => b.Amount) - totalExpenses
                    }).ToList();
                    BudgetSummaries = new ObservableCollection<object>(summaries);
                    break;

                case "Transaction History":
                    var txns = await _context.Transactions
                        .OrderByDescending(t => t.Date)
                        .ToListAsync();
                    TransactionHistory = new ObservableCollection<Transaction>(txns);
                    break;

                case "Parameters List":
                    var @params = await _context.Parameters.OrderBy(p => p.Name).ToListAsync();
                    ParametersList = new ObservableCollection<Parameter>(@params);
                    break;

                case "Office Budget Allocation":
                    var allocations = await _context.OfficeAllocations
                        .Include(a => a.Office)
                        .Include(a => a.YearlyBudget)
                        .OrderBy(a => a.Office!.Name)
                        .ToListAsync();
                    var officeSummaries = allocations.Select(a => new
                    {
                        DepartmentName = a.Office?.Name ?? "Unknown",
                        Year           = a.YearlyBudget?.Year.ToString() ?? "N/A",
                        Allocated      = a.AllocatedAmount,
                    }).ToList();
                    DepartmentalBudgets = new ObservableCollection<object>(officeSummaries);
                    break;

                case "System Overview":
                    var totalUsers     = await _context.Users.CountAsync();
                    var totalBudget    = await _context.Budgets.SumAsync(b => b.Amount);
                    var totalExp       = await _context.Transactions
                        .Where(t => t.TransactionType == "Expense")
                        .SumAsync(t => t.Amount ?? 0);
                    var totalConsolidated = await _context.ConsolidatedTransactions.CountAsync();
                    SystemOverview = new ObservableCollection<object>
                    {
                        new { Metric = "Total Registered Users",          Value = totalUsers.ToString() },
                        new { Metric = "Total Budget Allocated",          Value = totalBudget.ToString("C") },
                        new { Metric = "Total Expenses (Dept)",           Value = totalExp.ToString("C") },
                        new { Metric = "Total Dept Transactions",         Value = (await _context.Transactions.CountAsync()).ToString() },
                        new { Metric = "Total Consolidated Transactions", Value = totalConsolidated.ToString() },
                    };
                    break;
            }
        }
        catch (Exception ex)
        {
            // Only show dialog if the UI is still alive (not during shutdown)
            if (System.Windows.Application.Current != null)
            {
                System.Windows.MessageBox.Show(
                    $"Error generating report:\n{ex.Message}", "Report Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ReportsViewModel] Silent error during shutdown: {ex.Message}");
            }
        }
    }

    // ── Financial Overview ────────────────────────────────────────────────────
    private async Task LoadFinancialOverviewAsync()
    {
        TotalBudget    = await _context.YearlyBudgets.SumAsync(b => b.TotalAmount);
        TotalExpenses  = await _context.Transactions.SumAsync(t => t.Amount ?? 0);
        ActiveUsers    = await _context.Users.CountAsync(u => u.Status == "active");
        TotalProjects  = await _context.ProjectDetails.CountAsync();

        // Dept budget distribution pie
        var officeData = await _context.OfficeAllocations
            .Include(a => a.Office)
            .GroupBy(a => a.Office!.Name)
            .Select(g => new { Name = g.Key ?? "Unknown", Amount = (double)g.Sum(a => a.AllocatedAmount) })
            .ToListAsync();

        var deptSeries = new SeriesCollection();
        foreach (var d in officeData)
            deptSeries.Add(new PieSeries { Title = d.Name, Values = new ChartValues<double> { d.Amount }, DataLabels = true });
        DeptBudgetSeries = deptSeries;

        // Project budget pie
        var currentYear = DateTime.Now.Year;
        var projectData = await _context.ProjectDetails
            .Join(_context.YearlyBudgets,
                p => p.YearlyBudgetId, y => y.Id,
                (p, y) => new { p, y })
            .Where(x => x.y.Year == currentYear)
            .GroupBy(x => x.p.Name)
            .Select(g => new { Name = g.Key, Amount = (double)g.Sum(x => x.p.Budget ?? 0) })
            .ToListAsync();

        var projSeries = new SeriesCollection();
        foreach (var p in projectData)
            projSeries.Add(new PieSeries { Title = p.Name, Values = new ChartValues<double> { p.Amount }, DataLabels = true });
        ProjectBudgetSeries = projSeries;
    }

    // ── Consolidated Transactions Analytics ───────────────────────────────────
    private async Task LoadConsolidatedAnalyticsAsync()
    {
        var all = await _context.ConsolidatedTransactions.ToListAsync();

        ConsolidatedTotalCount  = all.Count;
        ConsolidatedTotalAmount = all.Sum(ct => ct.Amount ?? 0);
        ConsolidatedAvgAmount   = ConsolidatedTotalCount > 0
            ? ConsolidatedTotalAmount / ConsolidatedTotalCount : 0;

        // Pie — by transaction type
        var typeSeries = new SeriesCollection();
        foreach (var grp in all.GroupBy(ct => ct.TransactionType ?? "Unknown")
                               .Select(g => new { Type = g.Key, Amount = g.Sum(x => x.Amount ?? 0) }))
        {
            typeSeries.Add(new PieSeries
            {
                Title      = grp.Type,
                Values     = new ChartValues<decimal> { grp.Amount },
                DataLabels = true
            });
        }
        ConsolidatedTypeSeries = typeSeries;

        // Bar — monthly trend
        var monthly = all
            .Where(ct => ct.TransactionDate.HasValue)
            .GroupBy(ct => new { ct.TransactionDate!.Value.Year, ct.TransactionDate.Value.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Label  = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
                Amount = g.Sum(x => x.Amount ?? 0)
            })
            .ToList();

        var monthValues = new ChartValues<decimal>();
        var monthLabels = new ObservableCollection<string>();
        foreach (var m in monthly) { monthValues.Add(m.Amount); monthLabels.Add(m.Label); }

        ConsolidatedMonthlyLabels = monthLabels;
        ConsolidatedMonthlySeries = new SeriesCollection
        {
            new ColumnSeries { Title = "Monthly Amount", Values = monthValues }
        };
    }

    // ── CRS Beneficiary Analytics (raw SQL via separate connection) ───────────
    private async Task LoadCrsAnalyticsAsync()
    {
        try
        {
            using var conn = new MySqlConnection(DatabaseConfig.CrsConnectionString);
            await conn.OpenAsync();

            // Aggregate query — count, PWD, senior, gender, age groups
            const string sql = @"
                SELECT
                    COUNT(*)                              AS total,
                    SUM(is_pwd)                           AS pwd_count,
                    SUM(is_senior)                        AS senior_count,
                    SUM(CASE WHEN LOWER(sex)='male'   THEN 1 ELSE 0 END) AS male_count,
                    SUM(CASE WHEN LOWER(sex)='female' THEN 1 ELSE 0 END) AS female_count,
                    SUM(CASE WHEN CAST(age AS UNSIGNED) BETWEEN  0 AND 17  THEN 1 ELSE 0 END) AS age_0_17,
                    SUM(CASE WHEN CAST(age AS UNSIGNED) BETWEEN 18 AND 35  THEN 1 ELSE 0 END) AS age_18_35,
                    SUM(CASE WHEN CAST(age AS UNSIGNED) BETWEEN 36 AND 60  THEN 1 ELSE 0 END) AS age_36_60,
                    SUM(CASE WHEN CAST(age AS UNSIGNED) > 60               THEN 1 ELSE 0 END) AS age_60_plus
                FROM val_beneficiaries;";

            using var cmd    = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                CrsTotalCount  = reader.GetInt32("total");
                CrsPwdCount    = reader.GetInt32("pwd_count");
                CrsSeniorCount = reader.GetInt32("senior_count");

                int male   = reader.GetInt32("male_count");
                int female = reader.GetInt32("female_count");
                int other  = CrsTotalCount - male - female;

                // Gender pie
                var genderSeries = new SeriesCollection();
                if (male   > 0) genderSeries.Add(new PieSeries { Title = "Male",   Values = new ChartValues<int> { male },   DataLabels = true });
                if (female > 0) genderSeries.Add(new PieSeries { Title = "Female", Values = new ChartValues<int> { female }, DataLabels = true });
                if (other  > 0) genderSeries.Add(new PieSeries { Title = "Other",  Values = new ChartValues<int> { other },  DataLabels = true });
                CrsGenderSeries = genderSeries;

                // Age histogram
                var ageCounts = new int[]
                {
                    reader.GetInt32("age_0_17"),
                    reader.GetInt32("age_18_35"),
                    reader.GetInt32("age_36_60"),
                    reader.GetInt32("age_60_plus")
                };
                var ageValues = new ChartValues<int>(ageCounts);
                CrsAgeGroupSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title  = "Beneficiaries",
                        Values = ageValues
                    }
                };
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"CRS connection error:\n{ex.Message}\n\nCheck your CRS database settings.",
                "CRS Analytics Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
