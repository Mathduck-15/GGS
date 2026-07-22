using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;

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
        "System Overview",
        "Beneficiaries per Project",
        "Individual Beneficiaries Services Received",
        "Budget Utilization Report",
        "Project Implementation Status Report",
        "Public Service Delivery Report",
        "Citizen Feedback Summary Report",
        "Beneficiary Master List"
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
    public bool IsFinancialOverviewVisible              => SelectedReportType == "Financial Overview";
    public bool IsConsolidatedAnalyticsVisible          => SelectedReportType == "Consolidated Transactions Analytics";
    public bool IsCrsAnalyticsVisible                   => SelectedReportType == "CRS Beneficiary Analytics";
    public bool IsUserActivityLogVisible                => SelectedReportType == "User Activity Log";
    public bool IsBudgetSummaryVisible                  => SelectedReportType == "Budget Summary by Category";
    public bool IsTransactionHistoryVisible             => SelectedReportType == "Transaction History";
    public bool IsParametersListVisible                 => SelectedReportType == "Parameters List";
    public bool IsDepartmentalBudgetVisible             => SelectedReportType == "Office Budget Allocation";
    public bool IsSystemOverviewVisible                 => SelectedReportType == "System Overview";
    public bool IsBeneficiariesPerProjectVisible        => SelectedReportType == "Beneficiaries per Project";
    public bool IsIndividualBeneficiariesVisible        => SelectedReportType == "Individual Beneficiaries Services Received";
    public bool IsBudgetUtilizationVisible              => SelectedReportType == "Budget Utilization Report";
    public bool IsProjectStatusVisible                  => SelectedReportType == "Project Implementation Status Report";
    public bool IsPublicServiceDeliveryVisible          => SelectedReportType == "Public Service Delivery Report";
    public bool IsCitizenFeedbackVisible                => SelectedReportType == "Citizen Feedback Summary Report";
    public bool IsBeneficiaryMasterListVisible          => SelectedReportType == "Beneficiary Master List";

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

    private ObservableCollection<TblTransaction> _transactionHistory = new();
    public ObservableCollection<TblTransaction>  TransactionHistory  { get => _transactionHistory;  set { _transactionHistory  = value; OnPropertyChanged(); } }

    private ObservableCollection<Parameter> _parametersList       = new();
    public ObservableCollection<Parameter>  ParametersList        { get => _parametersList;       set { _parametersList       = value; OnPropertyChanged(); } }

    private ObservableCollection<object>    _departmentalBudgets  = new();
    public ObservableCollection<object>     DepartmentalBudgets   { get => _departmentalBudgets;  set { _departmentalBudgets  = value; OnPropertyChanged(); } }

    private ObservableCollection<object>    _systemOverview       = new();
    public ObservableCollection<object>     SystemOverview        { get => _systemOverview;       set { _systemOverview       = value; OnPropertyChanged(); } }

    // ── Beneficiaries per Project ─────────────────────────────────────────────
    private ObservableCollection<object> _beneficiariesPerProject = new();
    public ObservableCollection<object>  BeneficiariesPerProject  { get => _beneficiariesPerProject; set { _beneficiariesPerProject = value; OnPropertyChanged(); } }

    private SeriesCollection _bppBarSeries = new();
    public SeriesCollection  BppBarSeries  { get => _bppBarSeries; set { _bppBarSeries = value; OnPropertyChanged(); } }

    private ObservableCollection<string> _bppBarLabels = new();
    public ObservableCollection<string>  BppBarLabels  { get => _bppBarLabels; set { _bppBarLabels = value; OnPropertyChanged(); } }

    // ── Individual Beneficiaries Services Received ────────────────────────────
    private ObservableCollection<object> _individualBeneficiaries = new();
    public ObservableCollection<object>  IndividualBeneficiaries  { get => _individualBeneficiaries; set { _individualBeneficiaries = value; OnPropertyChanged(); } }

    // ── Budget Utilization Report ─────────────────────────────────────────────
    private ObservableCollection<object> _budgetUtilization = new();
    public ObservableCollection<object>  BudgetUtilization  { get => _budgetUtilization; set { _budgetUtilization = value; OnPropertyChanged(); } }

    private SeriesCollection _budgetUtilSeries = new();
    public SeriesCollection  BudgetUtilSeries  { get => _budgetUtilSeries; set { _budgetUtilSeries = value; OnPropertyChanged(); } }

    private ObservableCollection<string> _budgetUtilLabels = new();
    public ObservableCollection<string>  BudgetUtilLabels  { get => _budgetUtilLabels; set { _budgetUtilLabels = value; OnPropertyChanged(); } }

    // ── Project Implementation Status Report ──────────────────────────────────
    private ObservableCollection<object> _projectStatusRows = new();
    public ObservableCollection<object>  ProjectStatusRows  { get => _projectStatusRows; set { _projectStatusRows = value; OnPropertyChanged(); } }

    private SeriesCollection _projectStatusSeries = new();
    public SeriesCollection  ProjectStatusSeries  { get => _projectStatusSeries; set { _projectStatusSeries = value; OnPropertyChanged(); } }

    private int _activeProjectCount;
    public int ActiveProjectCount { get => _activeProjectCount; set { _activeProjectCount = value; OnPropertyChanged(); } }

    private int _closedProjectCount;
    public int ClosedProjectCount { get => _closedProjectCount; set { _closedProjectCount = value; OnPropertyChanged(); } }

    // ── Public Service Delivery Report ────────────────────────────────────────
    private ObservableCollection<object> _publicServiceRows = new();
    public ObservableCollection<object>  PublicServiceRows  { get => _publicServiceRows; set { _publicServiceRows = value; OnPropertyChanged(); } }

    private SeriesCollection _publicServiceSeries = new();
    public SeriesCollection  PublicServiceSeries  { get => _publicServiceSeries; set { _publicServiceSeries = value; OnPropertyChanged(); } }

    private ObservableCollection<string> _publicServiceLabels = new();
    public ObservableCollection<string>  PublicServiceLabels  { get => _publicServiceLabels; set { _publicServiceLabels = value; OnPropertyChanged(); } }

    // ── Citizen Feedback Summary Report ──────────────────────────────────────
    private ObservableCollection<object> _citizenFeedbackRows = new();
    public ObservableCollection<object>  CitizenFeedbackRows  { get => _citizenFeedbackRows; set { _citizenFeedbackRows = value; OnPropertyChanged(); } }

    private double _avgFeedbackScore;
    public double AvgFeedbackScore { get => _avgFeedbackScore; set { _avgFeedbackScore = value; OnPropertyChanged(); } }

    private int _totalFeedbackCount;
    public int TotalFeedbackCount  { get => _totalFeedbackCount; set { _totalFeedbackCount = value; OnPropertyChanged(); } }

    private SeriesCollection _feedbackScoreSeries = new();
    public SeriesCollection  FeedbackScoreSeries  { get => _feedbackScoreSeries; set { _feedbackScoreSeries = value; OnPropertyChanged(); } }

    private ObservableCollection<string> _feedbackScoreLabels = new();
    public ObservableCollection<string>  FeedbackScoreLabels  { get => _feedbackScoreLabels; set { _feedbackScoreLabels = value; OnPropertyChanged(); } }

    // ── Beneficiary Master List ───────────────────────────────────────────────
    private ObservableCollection<object> _beneficiaryMasterList = new();
    public ObservableCollection<object>  BeneficiaryMasterList  { get => _beneficiaryMasterList; set { _beneficiaryMasterList = value; OnPropertyChanged(); } }

    private int _bmlTotalBeneficiaries;
    public int BmlTotalBeneficiaries { get => _bmlTotalBeneficiaries; set { _bmlTotalBeneficiaries = value; OnPropertyChanged(); } }

    private decimal _bmlTotalAmount;
    public decimal BmlTotalAmount { get => _bmlTotalAmount; set { _bmlTotalAmount = value; OnPropertyChanged(); } }

    private int _bmlPwdCount;
    public int BmlPwdCount { get => _bmlPwdCount; set { _bmlPwdCount = value; OnPropertyChanged(); } }

    private int _bmlSeniorCount;
    public int BmlSeniorCount { get => _bmlSeniorCount; set { _bmlSeniorCount = value; OnPropertyChanged(); } }

    private SeriesCollection _bmlGenderSeries = new();
    public SeriesCollection  BmlGenderSeries  { get => _bmlGenderSeries; set { _bmlGenderSeries = value; OnPropertyChanged(); } }

    private SeriesCollection _bmlClassificationSeries = new();
    public SeriesCollection  BmlClassificationSeries  { get => _bmlClassificationSeries; set { _bmlClassificationSeries = value; OnPropertyChanged(); } }

    private SeriesCollection _bmlTopBeneficiarySeries = new();
    public SeriesCollection  BmlTopBeneficiarySeries  { get => _bmlTopBeneficiarySeries; set { _bmlTopBeneficiarySeries = value; OnPropertyChanged(); } }

    private ObservableCollection<string> _bmlTopBeneficiaryLabels = new();
    public ObservableCollection<string>  BmlTopBeneficiaryLabels  { get => _bmlTopBeneficiaryLabels; set { _bmlTopBeneficiaryLabels = value; OnPropertyChanged(); } }

    private SeriesCollection _bmlMonthlyTrendSeries = new();
    public SeriesCollection  BmlMonthlyTrendSeries  { get => _bmlMonthlyTrendSeries; set { _bmlMonthlyTrendSeries = value; OnPropertyChanged(); } }

    private ObservableCollection<string> _bmlMonthlyTrendLabels = new();
    public ObservableCollection<string>  BmlMonthlyTrendLabels  { get => _bmlMonthlyTrendLabels; set { _bmlMonthlyTrendLabels = value; OnPropertyChanged(); } }

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand GenerateReportCommand { get; }
    public ICommand PrintExportCommand    { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public ReportsViewModel()
    {
        try { _context = App.AppHost!.Services.GetRequiredService<AppDbContext>(); }
        catch { }

        GenerateReportCommand = new RelayCommand(async _ => await GenerateReportAsync());
        PrintExportCommand    = new RelayCommand(_ =>
        {
            if (!string.IsNullOrEmpty(SelectedReportType))
                HtmlReportExporter.ExportAndOpen(SelectedReportType, this);
        });

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
        OnPropertyChanged(nameof(IsBeneficiariesPerProjectVisible));
        OnPropertyChanged(nameof(IsIndividualBeneficiariesVisible));
        OnPropertyChanged(nameof(IsBudgetUtilizationVisible));
        OnPropertyChanged(nameof(IsProjectStatusVisible));
        OnPropertyChanged(nameof(IsPublicServiceDeliveryVisible));
        OnPropertyChanged(nameof(IsCitizenFeedbackVisible));
        OnPropertyChanged(nameof(IsBeneficiaryMasterListVisible));

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
                    var totalExpenses = await _context.TblTransactions
                        .Where(t => t.TransactionType == "Expense" || t.TransactionType == "disbursement")
                        .SumAsync(t => t.Amount);
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
                    var txns = await _context.TblTransactions
                        .OrderByDescending(t => t.TransactionDate)
                        .ToListAsync();
                    TransactionHistory = new ObservableCollection<TblTransaction>(txns);
                    break;

                case "Parameters List":
                    var @params = await _context.Parameters.OrderBy(p => p.Name).ToListAsync();
                    ParametersList = new ObservableCollection<Parameter>(@params);
                    break;

                case "Office Budget Allocation":
                    var allocations = await _context.BudgetAllocations
                        .Include(a => a.Office)
                        .Include(a => a.MasterBudget)
                        .ToListAsync();
                    var officesList = await _context.Offices.ToListAsync();
                    var officeSummaries = allocations.Select(a => new
                    {
                        DepartmentName = a.Office?.Name ?? officesList.FirstOrDefault(o => o.Id == a.OfficeId)?.Name ?? "Unknown",
                        Year           = a.MasterBudget?.FiscalYear ?? "N/A",
                        Allocated      = a.AllocatedAmount,
                    }).OrderBy(a => a.DepartmentName).ToList();
                    DepartmentalBudgets = new ObservableCollection<object>(officeSummaries);
                    break;

                case "System Overview":
                    var totalUsers     = await _context.Users.CountAsync();
                    var totalBudget    = await _context.Budgets.SumAsync(b => b.Amount);
                    var totalExp       = await _context.TblTransactions
                        .Where(t => t.TransactionType == "Expense" || t.TransactionType == "disbursement")
                        .SumAsync(t => t.Amount);
                    var totalConsolidated = await _context.ConsolidatedTransactions.CountAsync();
                    SystemOverview = new ObservableCollection<object>
                    {
                        new { Metric = "Total Registered Users",          Value = totalUsers.ToString() },
                        new { Metric = "Total Budget Allocated",          Value = totalBudget.ToString("C") },
                        new { Metric = "Total Expenses (Dept)",           Value = totalExp.ToString("C") },
                        new { Metric = "Total Dept Transactions",         Value = (await _context.TblTransactions.CountAsync()).ToString() },
                        new { Metric = "Total Consolidated Transactions", Value = totalConsolidated.ToString() },
                    };
                    break;

                case "Beneficiaries per Project":
                    await LoadBeneficiariesPerProjectAsync();
                    break;

                case "Individual Beneficiaries Services Received":
                    await LoadIndividualBeneficiariesAsync();
                    break;

                case "Budget Utilization Report":
                    await LoadBudgetUtilizationAsync();
                    break;

                case "Project Implementation Status Report":
                    await LoadProjectStatusAsync();
                    break;

                case "Public Service Delivery Report":
                    await LoadPublicServiceDeliveryAsync();
                    break;

                case "Citizen Feedback Summary Report":
                    await LoadCitizenFeedbackAsync();
                    break;

                case "Beneficiary Master List":
                    await LoadBeneficiaryMasterListAsync();
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
        TotalBudget    = await _context.MasterBudgets.SumAsync(b => b.TotalAmount);
        TotalExpenses  = await _context.TblTransactions.SumAsync(t => t.Amount);
        ActiveUsers    = await _context.Users.CountAsync(u => u.Status == "active");
        TotalProjects  = await _context.ProjectDetails.CountAsync(p => p.Status == "active");

        // Dept budget distribution pie
        var officeData = await _context.BudgetAllocations
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
            .Where(p => p.Status == "active")
            .Join(_context.MasterBudgets,
                p => p.MasterBudgetId, y => y.Id,
                (p, y) => new { p, y })
            .Where(x => x.y.FiscalYear == currentYear.ToString())
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
            if (GoodGovernanceApp.Services.ConnectivityService.IsCrsOnline)
            {
                using var conn = new MySqlConnector.MySqlConnection(DatabaseConfig.CrsConnectionString);
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

                using var cmd    = new MySqlConnector.MySqlCommand(sql, conn);
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
            else
            {
                // OFFLINE MODE
                var cache = await _context.CrsBeneficiaryCaches.ToListAsync();

                CrsTotalCount = cache.Count;
                CrsPwdCount = cache.Count(c => c.IsPwd);
                CrsSeniorCount = cache.Count(c => c.IsSenior);

                int male = cache.Count(c => string.Equals(c.Sex, "male", StringComparison.OrdinalIgnoreCase));
                int female = cache.Count(c => string.Equals(c.Sex, "female", StringComparison.OrdinalIgnoreCase));
                int other = CrsTotalCount - male - female;

                var genderSeries = new SeriesCollection();
                if (male > 0) genderSeries.Add(new PieSeries { Title = "Male", Values = new ChartValues<int> { male }, DataLabels = true });
                if (female > 0) genderSeries.Add(new PieSeries { Title = "Female", Values = new ChartValues<int> { female }, DataLabels = true });
                if (other > 0) genderSeries.Add(new PieSeries { Title = "Other", Values = new ChartValues<int> { other }, DataLabels = true });
                CrsGenderSeries = genderSeries;

                var ageCounts = new int[]
                {
                    cache.Count(c => c.Age >= 0 && c.Age <= 17),
                    cache.Count(c => c.Age >= 18 && c.Age <= 35),
                    cache.Count(c => c.Age >= 36 && c.Age <= 60),
                    cache.Count(c => c.Age > 60)
                };
                CrsAgeGroupSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Beneficiaries (Cached)",
                        Values = new ChartValues<int>(ageCounts)
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

    // ── Beneficiaries per Project ─────────────────────────────────────────────
    private async Task LoadBeneficiariesPerProjectAsync()
    {
        var rows = await _context.ConsolidatedTransactions
            .Where(ct => ct.ProjectName != null)
            .GroupBy(ct => new { ct.ProjectName, ct.ProjectCode })
            .Select(g => new
            {
                ProjectName      = g.Key.ProjectName ?? "(no name)",
                ProjectCode      = g.Key.ProjectCode ?? "",
                BeneficiaryCount = g.Select(x => x.BeneficiaryId).Distinct().Count(),
                TotalAmount      = g.Sum(x => x.Amount ?? 0),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.BeneficiaryCount)
            .ToListAsync();

        BeneficiariesPerProject = new ObservableCollection<object>(rows.Cast<object>());

        // Bar chart
        var barValues = new ChartValues<int>(rows.Select(r => r.BeneficiaryCount));
        var barLabels = new ObservableCollection<string>(rows.Select(r =>
            r.ProjectName.Length > 20 ? r.ProjectName[..20] + "…" : r.ProjectName));

        BppBarLabels = barLabels;
        BppBarSeries = new SeriesCollection
        {
            new ColumnSeries { Title = "Beneficiaries", Values = barValues }
        };
    }

    // ── Individual Beneficiaries Services Received ────────────────────────────
    private async Task LoadIndividualBeneficiariesAsync()
    {
        var rows = await _context.ConsolidatedTransactions
            .Where(ct => ct.BeneficiaryId != null)
            .GroupBy(ct => new { ct.BeneficiaryId, ct.FullName })
            .Select(g => new
            {
                BeneficiaryId    = g.Key.BeneficiaryId ?? "",
                FullName         = g.Key.FullName ?? "(unknown)",
                ServicesReceived = g.Select(x => x.TransactionType).Distinct().Count(),
                TotalTransactions= g.Count(),
                TotalAmount      = g.Sum(x => x.Amount ?? 0),
                LastServiceDate  = g.Max(x => x.TransactionDate)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToListAsync();

        IndividualBeneficiaries = new ObservableCollection<object>(rows.Cast<object>());
    }

    // ── Budget Utilization Report ─────────────────────────────────────────────
    private async Task LoadBudgetUtilizationAsync()
    {
        var projects = await _context.ProjectDetails
            .Where(p => p.Status == "active" && p.ProjectDetailsID != null)
            .ToListAsync();

        var projectCodes = projects
            .Where(p => !string.IsNullOrEmpty(p.ProjectDetailsID))
            .Select(p => p.ProjectDetailsID!)
            .ToList();

        var spentByCode = await _context.ConsolidatedTransactions
            .Where(t => t.ProjectCode != null && projectCodes.Contains(t.ProjectCode) && (t.TransactionType == "Expense" || t.TransactionType == "disbursement"))
            .GroupBy(t => t.ProjectCode)
            .Select(g => new { Code = g.Key!, Spent = g.Sum(x => x.Amount ?? 0) })
            .ToListAsync();

        var spentDict = spentByCode.ToDictionary(x => x.Code, x => x.Spent);

        var rows = projects.Select(p =>
        {
            var budget  = p.Budget ?? 0;
            var spent   = p.ProjectDetailsID != null && spentDict.TryGetValue(p.ProjectDetailsID, out var s) ? s : 0;
            var util    = budget > 0 ? Math.Round((double)(spent / budget) * 100, 1) : 0.0;
            return new
            {
                ProjectName    = p.Name,
                ProjectCode    = p.ProjectDetailsID ?? "",
                Budget         = budget,
                Spent          = spent,
                Remaining      = budget - spent,
                UtilizationPct = util
            };
        }).OrderByDescending(x => x.UtilizationPct).ToList();

        BudgetUtilization = new ObservableCollection<object>(rows.Cast<object>());

        // Stacked bar: Budget vs Spent per project
        var budgetVals = new ChartValues<decimal>(rows.Select(r => r.Budget));
        var spentVals  = new ChartValues<decimal>(rows.Select(r => r.Spent));
        var labels     = new ObservableCollection<string>(rows.Select(r =>
            r.ProjectName.Length > 18 ? r.ProjectName[..18] + "…" : r.ProjectName));

        BudgetUtilLabels = labels;
        BudgetUtilSeries = new SeriesCollection
        {
            new StackedColumnSeries { Title = "Budget",  Values = budgetVals, Fill = System.Windows.Media.Brushes.SteelBlue  },
            new StackedColumnSeries { Title = "Spent",   Values = spentVals,  Fill = System.Windows.Media.Brushes.Tomato     }
        };
    }

    // ── Project Implementation Status Report ──────────────────────────────────
    private async Task LoadProjectStatusAsync()
    {
        var projects = await _context.ProjectDetails.ToListAsync();

        var projectCodes = projects
            .Where(p => !string.IsNullOrEmpty(p.ProjectDetailsID))
            .Select(p => p.ProjectDetailsID!)
            .ToList();

        var spentByCode = await _context.ConsolidatedTransactions
            .Where(t => t.ProjectCode != null && projectCodes.Contains(t.ProjectCode) && (t.TransactionType == "Expense" || t.TransactionType == "disbursement"))
            .GroupBy(t => t.ProjectCode)
            .Select(g => new { Code = g.Key!, Spent = g.Sum(x => x.Amount ?? 0) })
            .ToListAsync();

        var spentDict = spentByCode.ToDictionary(x => x.Code, x => x.Spent);

        var rows = projects.Select(p =>
        {
            var budget  = p.Budget ?? 0;
            var spent   = p.ProjectDetailsID != null && spentDict.TryGetValue(p.ProjectDetailsID, out var s) ? s : 0;
            var util    = budget > 0 ? Math.Round((double)(spent / budget) * 100, 1) : 0.0;
            return new
            {
                ProjectName    = p.Name,
                Office         = p.OfficeCode ?? "—",
                Status         = p.Status,
                Budget         = budget,
                Spent          = spent,
                Remaining      = budget - spent,
                UtilizationPct = util
            };
        }).OrderBy(x => x.Status).ThenByDescending(x => x.Budget).ToList();

        ProjectStatusRows = new ObservableCollection<object>(rows.Cast<object>());

        ActiveProjectCount = rows.Count(r => r.Status == "active");
        ClosedProjectCount = rows.Count(r => r.Status != "active");

        ProjectStatusSeries = new SeriesCollection
        {
            new PieSeries { Title = "Active", Values = new ChartValues<int> { ActiveProjectCount }, DataLabels = true },
            new PieSeries { Title = "Closed", Values = new ChartValues<int> { ClosedProjectCount }, DataLabels = true }
        };
    }

    // ── Public Service Delivery Report ────────────────────────────────────────
    private async Task LoadPublicServiceDeliveryAsync()
    {
        var rows = await _context.ConsolidatedTransactions
            .GroupBy(ct => new { ct.OfficeName, ct.OfficeId })
            .Select(g => new
            {
                OfficeName       = g.Key.OfficeName ?? g.Key.OfficeId ?? "Unknown Office",
                BeneficiaryCount = g.Select(x => x.BeneficiaryId).Distinct().Count(),
                TotalAmount      = g.Sum(x => x.Amount ?? 0),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.BeneficiaryCount)
            .ToListAsync();

        PublicServiceRows = new ObservableCollection<object>(rows.Cast<object>());

        // Bar chart — beneficiaries served per office
        var vals   = new ChartValues<int>(rows.Select(r => r.BeneficiaryCount));
        var labels = new ObservableCollection<string>(rows.Select(r =>
            r.OfficeName.Length > 18 ? r.OfficeName[..18] + "…" : r.OfficeName));

        PublicServiceLabels = labels;
        PublicServiceSeries = new SeriesCollection
        {
            new ColumnSeries { Title = "Beneficiaries Served", Values = vals }
        };
    }

    // ── Citizen Feedback Summary Report ──────────────────────────────────────
    private async Task LoadCitizenFeedbackAsync()
    {
        var evals = await _context.Evaluations
            .Include(e => e.Evaluator)
            .Include(e => e.UploadedFile)
            .OrderByDescending(e => e.EvaluationDate)
            .ToListAsync();

        TotalFeedbackCount = evals.Count;
        AvgFeedbackScore   = evals.Count > 0 ? Math.Round(evals.Average(e => (double)e.Score), 1) : 0;

        var rows = evals.Select(e => new
        {
            Date      = e.EvaluationDate.ToString("MMM dd, yyyy"),
            Evaluator = e.Evaluator?.Name ?? "—",
            File      = e.UploadedFile?.FileName ?? "—",
            Score     = e.Score,
            Rating    = e.Score >= 90 ? "Excellent" : e.Score >= 75 ? "Good" : e.Score >= 60 ? "Fair" : "Poor",
            Comments  = e.Comments ?? ""
        }).ToList();

        CitizenFeedbackRows = new ObservableCollection<object>(rows.Cast<object>());

        // Bar chart: score distribution buckets
        int excellent = evals.Count(e => e.Score >= 90);
        int good      = evals.Count(e => e.Score >= 75 && e.Score < 90);
        int fair      = evals.Count(e => e.Score >= 60 && e.Score < 75);
        int poor      = evals.Count(e => e.Score < 60);

        FeedbackScoreLabels = new ObservableCollection<string> { "Excellent (90+)", "Good (75–89)", "Fair (60–74)", "Poor (<60)" };
        FeedbackScoreSeries = new SeriesCollection
        {
            new ColumnSeries
            {
                Title  = "Evaluations",
                Values = new ChartValues<int> { excellent, good, fair, poor }
            }
        };
    }

    // ── Beneficiary Master List ───────────────────────────────────────────────
    private async Task LoadBeneficiaryMasterListAsync()
    {
        // Step 1: Aggregate beneficiary transaction summaries from consolidated_transactions
        var txSummaries = await _context.ConsolidatedTransactions
            .Where(ct => ct.BeneficiaryId != null)
            .GroupBy(ct => new
            {
                ct.BeneficiaryId,
                ct.FullName,
                ct.FirstName,
                ct.LastName,
                ct.MiddleName,
                ct.Barangay,
                ct.HouseholdNo
            })
            .Select(g => new
            {
                BeneficiaryId     = g.Key.BeneficiaryId!,
                FullName          = g.Key.FullName ?? $"{g.Key.LastName}, {g.Key.FirstName}",
                FirstName         = g.Key.FirstName ?? "",
                LastName          = g.Key.LastName  ?? "",
                Barangay          = g.Key.Barangay  ?? "",
                HouseholdNo       = g.Key.HouseholdNo ?? "",
                ServicesReceived  = g.Select(x => x.TransactionType).Distinct().Count(),
                TotalTransactions = g.Count(),
                TotalAmount       = g.Sum(x => x.Amount ?? 0),
                LastServiceDate   = g.Max(x => x.TransactionDate)
            })
            .ToListAsync();

        // Step 2: Enrich with CRS personal data
        var beneficiaryIds = txSummaries.Select(x => x.BeneficiaryId).ToList();

        // Dictionary: beneficiaryId -> (Sex, Age, Address, MaritalStatus, IsPwd, IsSenior)
        var profileDict = new Dictionary<string, (string Sex, string Age, string Address, string MaritalStatus, bool IsPwd, bool IsSenior)>(StringComparer.OrdinalIgnoreCase);

        if (GoodGovernanceApp.Services.ConnectivityService.IsCrsOnline)
        {
            // ── Online: fetch from CRS MySQL ────────────────────────────────
            try
            {
                using var conn = new MySqlConnector.MySqlConnection(DatabaseConfig.CrsConnectionString);
                await conn.OpenAsync();

                // Build parameterised IN clause
                var paramNames  = beneficiaryIds.Select((_, i) => $"@id{i}").ToList();
                var inClause    = string.Join(",", paramNames);
                var sql         = $@"
                    SELECT beneficiary_id, sex, age, address, marital_status, is_pwd, is_senior
                    FROM   val_beneficiaries
                    WHERE  beneficiary_id IN ({inClause});";

                using var cmd = new MySqlConnector.MySqlCommand(sql, conn);
                for (int i = 0; i < beneficiaryIds.Count; i++)
                    cmd.Parameters.AddWithValue($"@id{i}", beneficiaryIds[i]);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var bid = reader.IsDBNull(reader.GetOrdinal("beneficiary_id")) ? "" : reader.GetString("beneficiary_id");
                    if (string.IsNullOrEmpty(bid)) continue;

                    profileDict[bid] = (
                        Sex           : reader.IsDBNull(reader.GetOrdinal("sex"))            ? "" : reader.GetString("sex"),
                        Age           : reader.IsDBNull(reader.GetOrdinal("age"))            ? "" : reader.GetString("age"),
                        Address       : reader.IsDBNull(reader.GetOrdinal("address"))        ? "" : reader.GetString("address"),
                        MaritalStatus : reader.IsDBNull(reader.GetOrdinal("marital_status")) ? "" : reader.GetString("marital_status"),
                        IsPwd         : !reader.IsDBNull(reader.GetOrdinal("is_pwd"))  && reader.GetBoolean("is_pwd"),
                        IsSenior      : !reader.IsDBNull(reader.GetOrdinal("is_senior")) && reader.GetBoolean("is_senior")
                    );
                }
            }
            catch
            {
                // Fall through to cache on error
            }
        }

        // ── Offline / fallback: use local cache ──────────────────────────────
        if (profileDict.Count == 0)
        {
            var cache = await _context.CrsBeneficiaryCaches
                .Where(c => beneficiaryIds.Contains(c.BeneficiaryId))
                .ToListAsync();

            foreach (var c in cache)
            {
                profileDict[c.BeneficiaryId] = (
                    Sex           : c.Sex           ?? "",
                    Age           : c.Age.HasValue  ? c.Age.Value.ToString() : "",
                    Address       : c.Address       ?? "",
                    MaritalStatus : c.MaritalStatus ?? "",
                    IsPwd         : c.IsPwd,
                    IsSenior      : c.IsSenior
                );
            }
        }

        // Step 3: Build enriched master rows
        var masterRows = txSummaries.Select(t =>
        {
            profileDict.TryGetValue(t.BeneficiaryId, out var p);
            return new
            {
                BeneficiaryId     = t.BeneficiaryId,
                FullName          = string.IsNullOrWhiteSpace(t.FullName) ? "(unknown)" : t.FullName,
                Sex               = p.Sex           ?? "",
                Age               = p.Age           ?? "",
                Address           = p.Address       ?? "",
                MaritalStatus     = p.MaritalStatus ?? "",
                Barangay          = t.Barangay,
                HouseholdNo       = t.HouseholdNo,
                IsPwd             = p.IsPwd   ? "Yes" : "No",
                IsSenior          = p.IsSenior ? "Yes" : "No",
                ServicesReceived  = t.ServicesReceived,
                TotalTransactions = t.TotalTransactions,
                TotalAmount       = t.TotalAmount,
                LastServiceDate   = t.LastServiceDate.HasValue
                    ? t.LastServiceDate.Value.ToString("MMM dd, yyyy") : "—",
                // raw booleans for KPI counters
                _IsPwd            = p.IsPwd,
                _IsSenior         = p.IsSenior,
                _Sex              = (p.Sex ?? "").ToLower()
            };
        }).OrderByDescending(r => r.TotalAmount).ToList();

        BeneficiaryMasterList = new ObservableCollection<object>(masterRows.Cast<object>());

        // Step 4: KPIs
        BmlTotalBeneficiaries = masterRows.Count;
        BmlTotalAmount        = masterRows.Sum(r => r.TotalAmount);
        BmlPwdCount           = masterRows.Count(r => r._IsPwd);
        BmlSeniorCount        = masterRows.Count(r => r._IsSenior);

        // Step 5: Gender pie chart
        int male   = masterRows.Count(r => r._Sex == "male");
        int female = masterRows.Count(r => r._Sex == "female");
        int other  = masterRows.Count - male - female;

        var genderSeries = new SeriesCollection();
        if (male   > 0) genderSeries.Add(new PieSeries { Title = "Male",   Values = new ChartValues<int> { male },   DataLabels = true });
        if (female > 0) genderSeries.Add(new PieSeries { Title = "Female", Values = new ChartValues<int> { female }, DataLabels = true });
        if (other  > 0) genderSeries.Add(new PieSeries { Title = "Other",  Values = new ChartValues<int> { other },  DataLabels = true });
        BmlGenderSeries = genderSeries;

        // Step 6: Classification pie chart (PWD / Senior / Regular)
        int pwdOnly    = masterRows.Count(r => r._IsPwd && !r._IsSenior);
        int seniorOnly = masterRows.Count(r => r._IsSenior && !r._IsPwd);
        int both       = masterRows.Count(r => r._IsPwd && r._IsSenior);
        int regular    = masterRows.Count(r => !r._IsPwd && !r._IsSenior);

        var classSeries = new SeriesCollection();
        if (pwdOnly    > 0) classSeries.Add(new PieSeries { Title = "PWD Only",       Values = new ChartValues<int> { pwdOnly    }, DataLabels = true });
        if (seniorOnly > 0) classSeries.Add(new PieSeries { Title = "Senior Only",    Values = new ChartValues<int> { seniorOnly }, DataLabels = true });
        if (both       > 0) classSeries.Add(new PieSeries { Title = "PWD & Senior",   Values = new ChartValues<int> { both       }, DataLabels = true });
        if (regular    > 0) classSeries.Add(new PieSeries { Title = "Regular",        Values = new ChartValues<int> { regular    }, DataLabels = true });
        BmlClassificationSeries = classSeries;

        // Step 7: Top 10 by total amount — bar chart
        var top10 = masterRows.Take(10).ToList();
        BmlTopBeneficiaryLabels = new ObservableCollection<string>(
            top10.Select(r => r.FullName.Length > 20 ? r.FullName[..20] + "…" : r.FullName));
        BmlTopBeneficiarySeries = new SeriesCollection
        {
            new ColumnSeries
            {
                Title  = "Total Amount (₱)",
                Values = new ChartValues<decimal>(top10.Select(r => r.TotalAmount))
            }
        };

        // Step 8: Monthly transaction trend
        var monthly = await _context.ConsolidatedTransactions
            .Where(ct => ct.BeneficiaryId != null && ct.TransactionDate.HasValue)
            .GroupBy(ct => new { ct.TransactionDate!.Value.Year, ct.TransactionDate.Value.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                Count = g.Count(),
                Total = g.Sum(x => x.Amount ?? 0)
            })
            .ToListAsync();

        BmlMonthlyTrendLabels = new ObservableCollection<string>(monthly.Select(m =>
        {
            if (DateTime.TryParse(m.Label + "-01", out var dt))
                return dt.ToString("MMM yy");
            return m.Label;
        }));
        BmlMonthlyTrendSeries = new SeriesCollection
        {
            new ColumnSeries
            {
                Title  = "Transactions",
                Values = new ChartValues<int>(monthly.Select(m => m.Count)),
                Fill   = System.Windows.Media.Brushes.CornflowerBlue
            }
        };
    }
}
