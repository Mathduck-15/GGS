using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.ViewModels;

public class BudgetAllocationViewModel : ViewModelBase
{
    private readonly AppDbContext _context;

    // ── Yearly Budget Form ────────────────────────────────────────────────────
    private MasterBudget? _selectedMasterBudget;
    private decimal _unallocatedAmount;
    private decimal _allocationPercent;

    // ── Selected Office (for transactions sub-list) ───────────────────────────
    private OfficeAllocationItemViewModel? _selectedOffice;
    private string _transactionPanelHeader = string.Empty;

    public ObservableCollection<MasterBudget> MasterBudgets { get; } = new();
    public ObservableCollection<OfficeAllocationItemViewModel> DepartmentAllocations { get; } = new();
    public ObservableCollection<TblTransaction> OfficeTransactions { get; } = new();
    public ObservableCollection<ProjectDetail> OfficeProjects { get; } = new();

    // ── Properties ───────────────────────────────────────────────────────────
    // Removed Year creation properties

    public MasterBudget? SelectedMasterBudget
    {
        get => _selectedMasterBudget;
        set
        {
            _selectedMasterBudget = value;
            OnPropertyChanged();
            _ = LoadAllocationsAsync();
        }
    }

    public decimal UnallocatedAmount
    {
        get => _unallocatedAmount;
        set { _unallocatedAmount = value; OnPropertyChanged(); }
    }

    public decimal AllocationPercent
    {
        get => _allocationPercent;
        set { _allocationPercent = value; OnPropertyChanged(); }
    }

    public OfficeAllocationItemViewModel? SelectedOffice
    {
        get => _selectedOffice;
        set
        {
            _selectedOffice = value;
            OnPropertyChanged();
            _ = LoadOfficeTransactionsAsync();
        }
    }

    public string TransactionPanelHeader
    {
        get => _transactionPanelHeader;
        set { _transactionPanelHeader = value; OnPropertyChanged(); }
    }

    // ── Commands ─────────────────────────────────────────────────────────────
    public ICommand SaveAllocationsCommand { get; }
    public ICommand ViewProjectsCommand { get; }
    public ICommand AddProjectCommand { get; }

    public BudgetAllocationViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();

        SaveAllocationsCommand = new RelayCommand(async _ => await SaveAllocationsAsync(), _ => SelectedMasterBudget != null);
        ViewProjectsCommand = new RelayCommand(_ => OpenProjectsPopup(), _ => SelectedOffice != null && OfficeProjects.Any());
        AddProjectCommand = new RelayCommand(_ => OpenAddProjectWindow(), _ => SelectedOffice != null);

        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        var budgets = await _context.MasterBudgets.OrderByDescending(b => b.FiscalYear).ToListAsync();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            MasterBudgets.Clear();
            foreach (var b in budgets) MasterBudgets.Add(b);

            if (SelectedMasterBudget == null && MasterBudgets.Any())
            {
                SelectedMasterBudget = MasterBudgets.First();
            }
        });
    }

    public void InitializeWithBudget(MasterBudget budget)
    {
        SelectedMasterBudget = budget;
    }

    public void ActivateForOffice(string officeCode)
    {
        // Give the initial load a moment to fetch yearly budgets if it's currently running
        _ = Task.Run(async () =>
        {
            await Task.Delay(300); // Wait briefly for LoadInitialDataAsync to finish
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var officeVm = DepartmentAllocations.FirstOrDefault(a => a.OfficeCode == officeCode);
                if (officeVm != null)
                {
                    SelectedOffice = officeVm;
                }
            });
        });
    }

    // ── Data Loading ─────────────────────────────────────────────────────────

    private async Task LoadAllocationsAsync()
    {
        DepartmentAllocations.Clear();
        OfficeTransactions.Clear();
        TransactionPanelHeader = string.Empty;

        if (SelectedMasterBudget == null)
        {
            UnallocatedAmount = 0;
            return;
        }

        // Load offices + existing allocations
        var offices = await _context.Offices.OrderBy(o => o.OfficeCode).ToListAsync();
        var existingAllocations = await _context.BudgetAllocations
            .Where(a => a.MasterBudgetId == SelectedMasterBudget.Id)
            .ToListAsync();

        // Calculate SpentAmount per office from actual project transactions and consolidated_transactions
        var officeCodes = offices.Where(o => !string.IsNullOrEmpty(o.OfficeCode)).Select(o => o.OfficeCode).ToList();

        var officeIds = offices.Select(o => o.Id).ToList();

        var txSpentByOfficeId = new Dictionary<long, decimal>();
        if (officeIds.Any())
        {
            var projectSpent = await _context.TblTransactions
                .Where(t => t.OfficeId != null && officeIds.Contains(t.OfficeId.Value))
                .GroupBy(t => t.OfficeId!.Value)
                .Select(g => new { OfficeId = g.Key, Spent = g.Sum(x => x.Amount) })
                .ToListAsync();

            foreach (var item in projectSpent)
            {
                txSpentByOfficeId[item.OfficeId] = item.Spent;
            }
        }

        // 2. Add consolidated transactions directly linked to office code
        var consolidatedSpent = await _context.ConsolidatedTransactions
            .Where(c => c.OfficeId != null && officeCodes.Contains(c.OfficeId))
            .GroupBy(c => c.OfficeId)
            .Select(g => new { OfficeCode = g.Key, Spent = g.Sum(x => x.Amount ?? 0) })
            .ToDictionaryAsync(x => x.OfficeCode!, x => x.Spent);

        // 3. Build the final dictionary mapped by office.Id
        var spentByOffice = new Dictionary<long, decimal>();
        foreach (var office in offices)
        {
            if (string.IsNullOrEmpty(office.OfficeCode)) continue;

            decimal totalSpent = 0;
            if (txSpentByOfficeId.TryGetValue(office.Id, out decimal ts)) totalSpent += ts;
            if (consolidatedSpent.TryGetValue(office.OfficeCode, out decimal cs)) totalSpent += cs;

            spentByOffice[office.Id] = totalSpent;
        }

        foreach (var office in offices)
        {
            if (string.IsNullOrEmpty(office.OfficeCode)) continue;

            var allocation = existingAllocations.FirstOrDefault(a => a.OfficeId == office.Id)
                             ?? new BudgetAllocation { OfficeId = office.Id, MasterBudgetId = SelectedMasterBudget.Id, OfficeType = "service", Status = "active" };

            decimal spent = spentByOffice.TryGetValue(office.Id, out var s) ? s : 0m;

            var vm = new OfficeAllocationItemViewModel(allocation, office.Name, office.OfficeCode, spent);
            vm.AmountChanged += (_, _) => CalculateUnallocated();
            DepartmentAllocations.Add(vm);
        }

        CalculateUnallocated();
    }

    private async Task LoadOfficeTransactionsAsync()
    {
        OfficeTransactions.Clear();
        if (SelectedOffice == null) return;

        TransactionPanelHeader = $"Transactions — {SelectedOffice.DepartmentName} ({SelectedOffice.OfficeCode})";

        // Find office ID by matching office_code
        var office = await _context.Offices
            .FirstOrDefaultAsync(o => o.OfficeCode == SelectedOffice.OfficeCode);

        if (office == null) return;

        var txns = await _context.TblTransactions
            .Where(t => t.OfficeId == office.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .ToListAsync();

        foreach (var t in txns) OfficeTransactions.Add(t);

        // Load project breakdown
        OfficeProjects.Clear();
        var filteredProjects = await _context.ProjectDetails
            .Where(p => p.OfficeCode == SelectedOffice.OfficeCode)
            .ToListAsync();

        var projectCodes = filteredProjects
            .Where(p => !string.IsNullOrEmpty(p.ProjectDetailsID))
            .Select(p => p.ProjectDetailsID!)
            .ToList();

        var spentByProject = new Dictionary<string, decimal>();
        
        if (projectCodes.Any())
        {
            spentByProject = await _context.ConsolidatedTransactions
                .Where(t => t.ProjectCode != null && projectCodes.Contains(t.ProjectCode))
                .GroupBy(t => t.ProjectCode)
                .Select(g => new { ProjectCode = g.Key, Spent = g.Sum(x => x.Amount ?? 0) })
                .ToDictionaryAsync(x => x.ProjectCode!, x => x.Spent);
        }

        foreach (var project in filteredProjects)
        {
            if (project.ProjectDetailsID != null && spentByProject.TryGetValue(project.ProjectDetailsID, out decimal spent))
            {
                project.Spent = spent;
            }
            else
            {
                project.Spent = 0;
            }
            OfficeProjects.Add(project);
        }
    }

    private void CalculateUnallocated()
    {
        if (SelectedMasterBudget == null) return;
        decimal totalAllocated = DepartmentAllocations.Sum(a => a.Amount);
        UnallocatedAmount = SelectedMasterBudget.TotalAmount - totalAllocated;
        AllocationPercent = SelectedMasterBudget.TotalAmount > 0
            ? (totalAllocated / SelectedMasterBudget.TotalAmount) * 100
            : 0;
    }

    // Removed AddYearlyBudgetAsync

    // ── Save Allocations ──────────────────────────────────────────────────────
    private async Task SaveAllocationsAsync()
    {
        if (UnallocatedAmount < 0)
        {
            MessageBox.Show("Total allocations cannot exceed the master budget.");
            return;
        }

        foreach (var vm in DepartmentAllocations)
        {
            if (vm.Model.Id == 0 && vm.Amount > 0)
            {
                vm.Model.AllocatedAmount = vm.Amount;
                _context.BudgetAllocations.Add(vm.Model);
            }
            else if (vm.Model.Id != 0)
            {
                vm.Model.AllocatedAmount = vm.Amount;
                _context.Update(vm.Model);
            }
        }

        await _context.SaveChangesAsync();
        // Reload to refresh SpentAmount/Remaining
        await LoadAllocationsAsync();
        MessageBox.Show("Allocations saved successfully.");
    }

    private void OpenProjectsPopup()
    {
        if (SelectedOffice == null || !OfficeProjects.Any()) return;

        // Pass the whole ViewModel as DataContext so the popup can access OfficeProjects AND AddProjectCommand
        var window = new Views.DepartmentProjectsWindow(this)
        {
            Owner = System.Windows.Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault()
        };
        window.ShowDialog();
    }

    private void OpenAddProjectWindow()
    {
        if (SelectedOffice == null) return;

        var window = new Views.AddProjectWindow
        {
            Owner = System.Windows.Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault()
        };

        if (window.DataContext is AddProjectViewModel vm)
        {
            int year = DateTime.Now.Year;
            if (SelectedMasterBudget != null && int.TryParse(SelectedMasterBudget.FiscalYear, out int parsedYear))
            {
                year = parsedYear;
            }
            vm.SetContext(year, SelectedOffice.OfficeCode);
        }

        bool? result = window.ShowDialog();

        if (result == true)
        {
            _ = LoadOfficeTransactionsAsync(); // Refresh projects list
        }
    }
}

// ── Office Allocation Item ViewModel ─────────────────────────────────────────
public class OfficeAllocationItemViewModel : ViewModelBase
{
    public BudgetAllocation Model { get; }
    public string DepartmentName { get; }
    public string? OfficeCode { get; }

    private decimal _amount;
    private decimal _spentAmount;

    public decimal Amount
    {
        get => _amount;
        set
        {
            _amount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Remaining));
            OnPropertyChanged(nameof(AmountDisplay));
            OnPropertyChanged(nameof(RemainingDisplay));
            AmountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public decimal SpentAmount
    {
        get => _spentAmount;
        set
        {
            _spentAmount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Remaining));
            OnPropertyChanged(nameof(SpentDisplay));
            OnPropertyChanged(nameof(RemainingDisplay));
        }
    }

    /// <summary>Remaining = Allocated - Spent (from tbl_transaction)</summary>
    public decimal Remaining => Amount - SpentAmount;

    // ── Formatted display strings (handles decimal(65,30) trailing zeros) ──
    public string AmountDisplay    => Amount.ToString("N2");
    public string SpentDisplay     => SpentAmount.ToString("N2");
    public string RemainingDisplay => Remaining.ToString("N2");

    public event EventHandler? AmountChanged;

    public OfficeAllocationItemViewModel(BudgetAllocation model, string officeName, string? officeCode, decimal spentAmount = 0)
    {
        Model = model;
        DepartmentName = officeName;
        OfficeCode = officeCode;
        _amount = model.AllocatedAmount;
        _spentAmount = spentAmount;
    }
}

// Keep old name as alias for XAML compatibility
public class DepartmentAllocationViewModel : OfficeAllocationItemViewModel
{
    public DepartmentAllocationViewModel(BudgetAllocation model, string officeName, string? officeCode)
        : base(model, officeName, officeCode) { }
}
