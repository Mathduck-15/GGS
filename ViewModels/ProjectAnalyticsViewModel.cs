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
    public class ProjectAnalyticsViewModel : ViewModelBase
    {
        private readonly AppDbContext _dbContext;

        public string ProjectCode { get; }
        public string ProjectName { get; }

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

        public ObservableCollection<ConsolidatedTransactionsViewModel> ProjectTransactions { get; } = new();

        private SeriesCollection _typeBreakdownSeries = new();
        public SeriesCollection TypeBreakdownSeries
        {
            get => _typeBreakdownSeries;
            set { _typeBreakdownSeries = value; OnPropertyChanged(); }
        }

        public ProjectAnalyticsViewModel(AppDbContext dbContext, string projectCode, string projectName)
        {
            _dbContext = dbContext;
            ProjectCode = projectCode;
            ProjectName = string.IsNullOrWhiteSpace(projectName) ? "Project" : projectName;

            _ = LoadAnalyticsAsync();
        }

        private async Task LoadAnalyticsAsync()
        {
            IsLoading = true;
            try
            {
                // 1. Load department budget transactions (transactions table)
                var departmentTransactions = await _dbContext.Transactions
                    .Where(t => t.ProjectCode == ProjectCode)
                    .ToListAsync();

                // 2. Load consolidated transactions
                var consolidatedTransactions = await _dbContext.ConsolidatedTransactions
                    .Where(ct => ct.ProjectCode == ProjectCode)
                    .ToListAsync();

                var projectDetails = await _dbContext.ProjectDetails
                    .FirstOrDefaultAsync(p => p.ProjectDetailsID == ProjectCode);

                // 3. Map dept transactions
                var deptList = departmentTransactions.Select(t => new ConsolidatedTransactionsViewModel
                {
                    Id              = t.Id,
                    ProjectCode     = t.ProjectCode ?? string.Empty,
                    BeneficiaryId   = projectDetails?.ContactPerson ?? "Unknown",
                    FullName        = "Department Project",
                    TransactionType = t.TransactionType ?? "Department Project",
                    Amount          = t.Amount ?? 0,
                    TransactionDate = t.Date,
                    Status          = "Completed",
                    Source          = "Department Budget",
                    CreatedAt       = t.Date ?? DateTime.MinValue,
                    OfficeId        = projectDetails?.OfficeCode ?? string.Empty,
                    OfficeName      = string.Empty
                }).ToList();

                // 4. Map consolidated transactions
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
                    OfficeId        = ct.OfficeId ?? string.Empty,
                    OfficeName      = ct.OfficeName ?? string.Empty
                }).ToList();

                // 5. Merge lists
                var combinedList = deptList.Concat(consolidatedList).ToList();

                // 6. Set stats
                TotalTransactions = combinedList.Count;
                TotalAmount       = combinedList.Sum(t => t.Amount);
                AverageAmount     = TotalTransactions > 0 ? TotalAmount / TotalTransactions : 0;

                // 7. Populate list
                foreach (var r in combinedList.OrderByDescending(t => t.CreatedAt))
                    ProjectTransactions.Add(r);

                if (!combinedList.Any()) return;

                // 8. Pie Chart - Amount by Transaction Type
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
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error loading project analytics:\n{ex}",
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
