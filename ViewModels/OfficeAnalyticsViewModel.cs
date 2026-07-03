using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;

namespace GoodGovernanceApp.ViewModels
{
    public class OfficeAnalyticsViewModel : ViewModelBase
    {
        private readonly AppDbContext _dbContext;

        public string OfficeCode { get; }
        public string OfficeName { get; }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private int _totalTransactions;
        public int TotalTransactions
        {
            get => _totalTransactions;
            set { _totalTransactions = value; OnPropertyChanged(); }
        }

        private decimal _consolidatedAmount;
        public decimal ConsolidatedAmount
        {
            get => _consolidatedAmount;
            set { _consolidatedAmount = value; OnPropertyChanged(); }
        }

        private decimal _departmentBudgetAmount;
        public decimal DepartmentBudgetAmount
        {
            get => _departmentBudgetAmount;
            set { _departmentBudgetAmount = value; OnPropertyChanged(); }
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

        public ObservableCollection<ConsolidatedTransactionsViewModel> OfficeTransactions { get; } = new();

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

        public OfficeAnalyticsViewModel(AppDbContext dbContext, string officeCode, string officeName)
        {
            _dbContext = dbContext;
            OfficeCode = officeCode;
            OfficeName = string.IsNullOrWhiteSpace(officeName) ? "Office" : officeName;
            Formatter = value => value.ToString("C");

            _ = LoadAnalyticsAsync();
        }

        private async Task LoadAnalyticsAsync()
        {
            IsLoading = true;
            try
            {
                // 1. Get all projects belonging to this office
                var linkedProjects = await _dbContext.ProjectDetails
                    .Where(pd => pd.OfficeCode == OfficeCode && pd.ProjectDetailsID != null)
                    .ToListAsync();

                var projectCodes = linkedProjects
                    .Select(pd => pd.ProjectDetailsID!)
                    .ToList();

                // 2. Load department budget transactions (transactions table)
                var departmentTransactions = projectCodes.Any()
                    ? await _dbContext.Transactions
                        .Where(t => t.ProjectCode != null && projectCodes.Contains(t.ProjectCode))
                        .ToListAsync()
                    : new System.Collections.Generic.List<Transaction>();

                // 3. Load consolidated transactions by office_id
                var consolidatedTransactions = await _dbContext.ConsolidatedTransactions
                    .Where(ct => ct.OfficeId == OfficeCode)
                    .ToListAsync();

                // 4. Map dept transactions
                var deptList = departmentTransactions.Select(t => new ConsolidatedTransactionsViewModel
                {
                    Id              = t.Id,
                    ProjectCode     = t.ProjectCode ?? string.Empty,
                    BeneficiaryId   = linkedProjects.FirstOrDefault(p => p.ProjectDetailsID == t.ProjectCode)?.ContactPerson ?? "Unknown",
                    FullName        = "Department Project",
                    TransactionType = t.TransactionType ?? "Department Project",
                    Amount          = t.Amount ?? 0,
                    TransactionDate = t.Date,
                    Status          = "Completed",
                    Source          = "Department Budget",
                    CreatedAt       = t.Date ?? DateTime.MinValue,
                    OfficeId        = OfficeCode,
                    OfficeName      = OfficeName
                }).ToList();

                // 5. Map consolidated transactions
                var consolidatedList = consolidatedTransactions.Select(ct => new ConsolidatedTransactionsViewModel
                {
                    Id              = ct.Id,
                    ProjectCode     = ct.ProjectCode ?? string.Empty,
                    BeneficiaryId   = ct.BeneficiaryId ?? string.Empty,
                    FullName        = ct.FullName ?? string.Empty,
                    TransactionType = ct.TransactionType ?? "Consolidated",
                    Amount          = ct.Amount ?? 0,
                    TransactionDate = ct.TransactionDate,
                    Status          = ct.Status ?? "Unknown",
                    Source          = "Consolidated",
                    CreatedAt       = ct.CreatedAt ?? DateTime.MinValue,
                    OfficeId        = ct.OfficeId ?? OfficeCode,
                    OfficeName      = ct.OfficeName ?? OfficeName
                }).ToList();

                // 6. Merge lists
                var combinedList = deptList.Concat(consolidatedList).ToList();

                // 7. Per-source summary stats
                TotalTransactions      = combinedList.Count;
                ConsolidatedAmount     = consolidatedList.Sum(t => t.Amount);
                DepartmentBudgetAmount = deptList.Sum(t => t.Amount);
                TotalAmount            = ConsolidatedAmount + DepartmentBudgetAmount;
                AverageAmount          = TotalTransactions > 0 ? TotalAmount / TotalTransactions : 0;

                // 8. Transactions list
                foreach (var r in combinedList.OrderByDescending(t => t.CreatedAt))
                    OfficeTransactions.Add(r);

                if (!combinedList.Any()) return;

                // 9. Pie Chart - Amount by Transaction Type
                var typeSeries = new SeriesCollection();
                foreach (var grp in combinedList
                    .GroupBy(t => t.TransactionType ?? "Unknown")
                    .Select(g => new { Type = g.Key, Amount = g.Sum(x => x.Amount) }))
                {
                    typeSeries.Add(new PieSeries
                    {
                        Title      = grp.Type,
                        Values     = new ChartValues<decimal> { grp.Amount },
                        DataLabels = true
                    });
                }
                TypeBreakdownSeries = typeSeries;

                // 10. Pie Chart - Source Breakdown
                var sourceSeries = new SeriesCollection();
                if (consolidatedList.Any())
                    sourceSeries.Add(new PieSeries
                    {
                        Title      = "Consolidated",
                        Values     = new ChartValues<int> { consolidatedList.Count },
                        DataLabels = true
                    });
                if (deptList.Any())
                    sourceSeries.Add(new PieSeries
                    {
                        Title      = "Department Budget",
                        Values     = new ChartValues<int> { deptList.Count },
                        DataLabels = true
                    });
                StatusBreakdownSeries = sourceSeries;

                // 11. Monthly Trend
                var allMonths = combinedList
                    .Where(t => t.TransactionDate.HasValue)
                    .Select(t => new { t.TransactionDate!.Value.Year, t.TransactionDate.Value.Month })
                    .Distinct()
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .ToList();

                var trendDept         = new ChartValues<decimal>();
                var trendConsolidated = new ChartValues<decimal>();
                var labels            = new ObservableCollection<string>();

                foreach (var m in allMonths)
                {
                    labels.Add($"{new DateTime(m.Year, m.Month, 1):MMM yyyy}");

                    trendDept.Add(deptList
                        .Where(t => t.TransactionDate.HasValue && t.TransactionDate.Value.Year == m.Year && t.TransactionDate.Value.Month == m.Month)
                        .Sum(t => t.Amount));

                    trendConsolidated.Add(consolidatedList
                        .Where(t => t.TransactionDate.HasValue && t.TransactionDate.Value.Year == m.Year && t.TransactionDate.Value.Month == m.Month)
                        .Sum(t => t.Amount));
                }

                MonthlyLabels = labels;
                MonthlyTrendSeries = new SeriesCollection
                {
                    new ColumnSeries { Title = "Department Budget", Values = trendDept         },
                    new ColumnSeries { Title = "Consolidated",      Values = trendConsolidated }
                };
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error loading office analytics:\n{ex}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
