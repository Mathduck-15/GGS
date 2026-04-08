using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LiveCharts;
using LiveCharts.Wpf;



namespace GoodGovernanceApp.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private AppDbContext _context;
    private decimal _totalBudget;
    private decimal _totalIncome;
    private decimal _totalExpense;
    private int _activeUsersCount;

    int currentYear = DateTime.Now.Year;

    // Custom chart data
    private double _incomeHeight;
    private double _expenseHeight;
    private List<DeptAllocationData> _deptAllocations = new List<DeptAllocationData>();


    private SeriesCollection _deptPieSeries = new SeriesCollection();
    private SeriesCollection _deptProject  = new SeriesCollection();

    public SeriesCollection DeptProjectPieSeries
    {
        get => _deptProject;
        set { _deptProject = value; OnPropertyChanged(); }
    }

    public SeriesCollection DeptPieSeries
    {
        get => _deptPieSeries;
        set { _deptPieSeries = value; OnPropertyChanged(); }
    }

    public decimal TotalBudget
    {
        get => _totalBudget;
        set { _totalBudget = value; OnPropertyChanged(); }
    }



    public decimal TotalIncome
    {
        get => _totalIncome;
        set { _totalIncome = value; OnPropertyChanged(); UpdateChartHeights(); }
    }

    public decimal TotalExpense
    {
        get => _totalExpense;
        set { _totalExpense = value; OnPropertyChanged(); UpdateChartHeights(); }
    }

    public int ActiveUsersCount
    {
        get => _activeUsersCount;
        set { _activeUsersCount = value; OnPropertyChanged(); }
    }

    public double IncomeHeight
    {
        get => _incomeHeight;
        set { _incomeHeight = value; OnPropertyChanged(); }
    }

    public double ExpenseHeight
    {
        get => _expenseHeight;
        set { _expenseHeight = value; OnPropertyChanged(); }
    }

    public List<DeptAllocationData> DeptAllocations
    {
        get => _deptAllocations;
        set { _deptAllocations = value; OnPropertyChanged(); }
    }

    public DashboardViewModel()
    {
        try
        {
            if (App.AppHost != null)
            {
                _context = App.AppHost.Services.GetRequiredService<AppDbContext>();
                _ = LoadAnalyticsAsync();
            }
        }
        catch { }
    }

    private async Task LoadAnalyticsAsync()
    {
        if (_context == null) return;

        try
        {
            TotalBudget = await _context.YearlyBudgets.SumAsync(b => b.TotalAmount);
            
            TotalIncome = await _context.Transactions
                                        .SumAsync(t => t.Amount);

            TotalExpense = await _context.Transactions
                                         .SumAsync(t => t.Amount);

            ActiveUsersCount = await _context.Users
                                             .CountAsync(u => u.Status == "active");


            var currentYear = DateTime.Now.Year;

            var projectData = await _context.ProjectDetails
                .Join(_context.YearlyBudgets,
                    p => p.YearlyBudgetId,
                    y => y.Id,
                    (p, y) => new { p, y })
                .Where(x => x.y.Year == currentYear)
                .GroupBy(x => x.p.Name)
                .Select(g => new DeptAllocationData
                {
                    Name = g.Key,
                    Amount = (double)g.Sum(x => x.p.Budget ?? 0)
                })
                .ToListAsync();

            var seriesproject = new SeriesCollection();

            foreach (var d in projectData)  // <-- use projectData now
            {
                seriesproject.Add(new LiveCharts.Wpf.PieSeries
                {
                    Title = d.Name,
                    Values = new ChartValues<double> { d.Amount },
                    DataLabels = true
                });
            }
            DeptProjectPieSeries = seriesproject;


            var officeData = await _context.OfficeAllocations
            .Include(a => a.Office)
            .GroupBy(a => a.Office!.Name)
            .Select(g => new DeptAllocationData { Name = g.Key, Amount = (double)g.Sum(a => a.AllocatedAmount) })
            .ToListAsync();

            var series = new SeriesCollection(); foreach (var d in officeData) { series.Add(new LiveCharts.Wpf.PieSeries
            {
                Title = d.Name,
                Values = new ChartValues<double> { d.Amount }, DataLabels = true
            }); } DeptPieSeries = series;




            UpdateChartHeights();
        }
        catch { }
    }

    private void UpdateChartHeights()
    {
        double incomeVal = (double)TotalIncome;
        double expenseVal = (double)TotalExpense;
        double max = Math.Max(incomeVal, expenseVal);
        
        if (max <= 0) 
        {
            IncomeHeight = 20; 
            ExpenseHeight = 20;
            return;
        }

        IncomeHeight = incomeVal / max * 250;
        ExpenseHeight = expenseVal / max * 250;
    }
}
