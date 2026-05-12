using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.ViewModels;

public class ReportsViewModel : ViewModelBase
{
    private readonly AppDbContext _context;

    public ObservableCollection<string> ReportTypes { get; } = new()
    {
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

    // Report Data Collections
    private ObservableCollection<SystemLog> _userActivityLogs = new();
    public ObservableCollection<SystemLog> UserActivityLogs
    {
        get => _userActivityLogs;
        set { _userActivityLogs = value; OnPropertyChanged(); }
    }

    private ObservableCollection<object> _budgetSummaries = new();
    public ObservableCollection<object> BudgetSummaries
    {
        get => _budgetSummaries;
        set { _budgetSummaries = value; OnPropertyChanged(); }
    }

    private ObservableCollection<Transaction> _transactionHistory = new();
    public ObservableCollection<Transaction> TransactionHistory
    {
        get => _transactionHistory;
        set { _transactionHistory = value; OnPropertyChanged(); }
    }

    private ObservableCollection<Parameter> _parametersList = new();
    public ObservableCollection<Parameter> ParametersList
    {
        get => _parametersList;
        set { _parametersList = value; OnPropertyChanged(); }
    }

    private ObservableCollection<object> _departmentalBudgets = new();
    public ObservableCollection<object> DepartmentalBudgets
    {
        get => _departmentalBudgets;
        set { _departmentalBudgets = value; OnPropertyChanged(); }
    }

    private ObservableCollection<object> _systemOverview = new();
    public ObservableCollection<object> SystemOverview
    {
        get => _systemOverview;
        set { _systemOverview = value; OnPropertyChanged(); }
    }

    // Visibility Properties for UI
    public bool IsUserActivityLogVisible => SelectedReportType == "User Activity Log";
    public bool IsBudgetSummaryVisible => SelectedReportType == "Budget Summary by Category";
    public bool IsTransactionHistoryVisible => SelectedReportType == "Transaction History";
    public bool IsParametersListVisible => SelectedReportType == "Parameters List";
    public bool IsDepartmentalBudgetVisible => SelectedReportType == "Office Budget Allocation";
    public bool IsSystemOverviewVisible => SelectedReportType == "System Overview";

    public ICommand GenerateReportCommand { get; }
    public ICommand PrintExportCommand { get; }

    public ReportsViewModel()
    {
        try
        {
            _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
        }
        catch { }

        GenerateReportCommand = new RelayCommand(async p => await GenerateReportAsync());
        PrintExportCommand = new RelayCommand(p => { /* Implement PDF/Excel export later */ });

        SelectedReportType = ReportTypes[0]; // Default selection
    }

    private async Task GenerateReportAsync()
    {
        try
        {
            // Reset visibilities
            OnPropertyChanged(nameof(IsUserActivityLogVisible));
            OnPropertyChanged(nameof(IsBudgetSummaryVisible));
            OnPropertyChanged(nameof(IsTransactionHistoryVisible));
            OnPropertyChanged(nameof(IsParametersListVisible));
            OnPropertyChanged(nameof(IsDepartmentalBudgetVisible));
            OnPropertyChanged(nameof(IsSystemOverviewVisible));

            switch (SelectedReportType)
            {
                case "User Activity Log":
                    var logs = await _context.SystemLogs.Include(l => l.User).OrderByDescending(l => l.Timestamp).ToListAsync();
                    UserActivityLogs = new ObservableCollection<SystemLog>(logs);
                    break;

                case "Budget Summary by Category":
                    // Note: Transaction has no CategoryId FK, so expenses cannot be
                    // broken down per category. We load each category's budget total
                    // and pair it with the global expense total from the transactions table.
                    var categories = await _context.Categories
                        .Include(c => c.Budgets)
                        .ToListAsync();
                    var totalExpenses = await _context.Transactions
                        .Where(t => t.TransactionType == "Expense")
                        .SumAsync(t => t.Amount ?? 0);
                    var summaries = categories.Select(c => new
                    {
                        CategoryName = c.Name,
                        TotalBudget = c.Budgets.Sum(b => b.Amount),
                        TotalExpenses = totalExpenses,
                        RemainingBalance = c.Budgets.Sum(b => b.Amount) - totalExpenses
                    }).ToList();
                    BudgetSummaries = new ObservableCollection<object>(summaries);
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
                        Year = a.YearlyBudget?.Year.ToString() ?? "N/A",
                        Allocated = a.AllocatedAmount,
                    }).ToList();
                    DepartmentalBudgets = new ObservableCollection<object>(officeSummaries);
                    break;

                case "System Overview":
                    var totalUsers = await _context.Users.CountAsync();
                    var totalBudgets = await _context.Budgets.SumAsync(b => b.Amount);
                    var totalExp = await _context.Transactions.Where(t => t.TransactionType == "Expense").SumAsync(t => t.Amount ?? 0);
                    
                    SystemOverview = new ObservableCollection<object>
                    {
                        new { Metric = "Total Registered Users", Value = totalUsers.ToString() },
                        new { Metric = "Total Budget Allocated", Value = totalBudgets.ToString("C") },
                        new { Metric = "Total Expenses", Value = totalExp.ToString("C") },
                        new { Metric = "Total Transactions", Value = (await _context.Transactions.CountAsync()).ToString() }
                    };
                    break;
            }
        }
        catch
        {
        }
    }
}
