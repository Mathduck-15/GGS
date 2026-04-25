using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GoodGovernanceApp.ViewModels;

public class BeneficiaryAnalyticsViewModel : ViewModelBase
{
    private readonly AppDbContext _dbContext;
    public string BeneficiaryId { get; }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private string _fullName = string.Empty;
    public string FullName
    {
        get => _fullName;
        set { _fullName = value; OnPropertyChanged(); }
    }

    private int _totalTransactions;
    public int TotalTransactions
    {
        get => _totalTransactions;
        set { _totalTransactions = value; OnPropertyChanged(); }
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set { _totalAmount = value; OnPropertyChanged(); }
    }

    private decimal _averageAmount;
    public decimal AverageAmount
    {
        get => _averageAmount;
        set { _averageAmount = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ConsolidatedTransactionsViewModel> RecentTransactions { get; } = new();

    // LiveCharts collections
    private SeriesCollection _typeBreakdownSeries = new();
    public SeriesCollection TypeBreakdownSeries
    {
        get => _typeBreakdownSeries;
        set { _typeBreakdownSeries = value; OnPropertyChanged(); }
    }

    private SeriesCollection _statusBreakdownSeries = new();
    public SeriesCollection StatusBreakdownSeries
    {
        get => _statusBreakdownSeries;
        set { _statusBreakdownSeries = value; OnPropertyChanged(); }
    }

    private SeriesCollection _monthlyTrendSeries = new();
    public SeriesCollection MonthlyTrendSeries
    {
        get => _monthlyTrendSeries;
        set { _monthlyTrendSeries = value; OnPropertyChanged(); }
    }

    private ObservableCollection<string> _monthlyLabels = new();
    public ObservableCollection<string> MonthlyLabels
    {
        get => _monthlyLabels;
        set { _monthlyLabels = value; OnPropertyChanged(); }
    }

    public Func<double, string> Formatter { get; set; }

    public BeneficiaryAnalyticsViewModel(AppDbContext dbContext, string beneficiaryId, string fullName)
    {
        _dbContext = dbContext;
        BeneficiaryId = beneficiaryId;
        FullName = fullName;
        Formatter = value => value.ToString("C");

        _ = LoadAnalyticsAsync();
    }

    private async Task LoadAnalyticsAsync()
    {
        IsLoading = true;
        try
        {
            var baseQuery = _dbContext.ConsolidatedTransactions
                .Where(t => t.BeneficiaryId == BeneficiaryId);

            // Fetch raw data needed for aggregations
            // We use ToListAsync() here because some aggregations (like grouping by DateOnly components) 
            // might not translate perfectly to all SQL dialects.
            var transactions = await baseQuery.ToListAsync();

            if (!transactions.Any()) return;

            TotalTransactions = transactions.Count;
            TotalAmount = transactions.Sum(t => t.Amount ?? 0);
            AverageAmount = TotalTransactions > 0 ? TotalAmount / TotalTransactions : 0;

            // Load recent transactions (top 10)
            var recent = transactions
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new ConsolidatedTransactionsViewModel
                {
                    Id = t.Id,
                    BeneficiaryId = t.BeneficiaryId ?? "",
                    FullName = t.FullName ?? "",
                    TransactionType = t.TransactionType ?? "",
                    Amount = t.Amount ?? 0,
                    TransactionDate = t.TransactionDate ?? DateOnly.MinValue,
                    Status = t.Status ?? ""
                });

            foreach (var r in recent)
            {
                RecentTransactions.Add(r);
            }

            // Pie Chart: Transaction Types
            var types = transactions
                .GroupBy(t => t.TransactionType ?? "Unknown")
                .Select(g => new { Type = g.Key, Amount = g.Sum(x => x.Amount ?? 0) })
                .ToList();

            var typeSeries = new SeriesCollection();
            foreach (var t in types)
            {
                typeSeries.Add(new PieSeries
                {
                    Title = t.Type,
                    Values = new ChartValues<decimal> { t.Amount },
                    DataLabels = true
                });
            }
            TypeBreakdownSeries = typeSeries;

            // Pie Chart: Status Breakdown
            var statuses = transactions
                .GroupBy(t => t.Status ?? "Unknown")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var statusSeries = new SeriesCollection();
            foreach (var s in statuses)
            {
                statusSeries.Add(new PieSeries
                {
                    Title = s.Status,
                    Values = new ChartValues<int> { s.Count },
                    DataLabels = true
                });
            }
            StatusBreakdownSeries = statusSeries;

            // Monthly Trend (Column or Line)
            var monthlyData = transactions
                .Where(t => t.TransactionDate.HasValue)
                .GroupBy(t => new { Year = t.TransactionDate!.Value.Year, Month = t.TransactionDate!.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Label = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
                    Amount = g.Sum(x => x.Amount ?? 0)
                })
                .ToList();

            var trendValues = new ChartValues<decimal>();
            var labels = new ObservableCollection<string>();

            foreach (var m in monthlyData)
            {
                trendValues.Add(m.Amount);
                labels.Add(m.Label);
            }

            MonthlyLabels = labels;
            MonthlyTrendSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Monthly Amount",
                    Values = trendValues
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading beneficiary analytics: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
