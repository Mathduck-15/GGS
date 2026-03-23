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
    private int _newBudgetYear = DateTime.Now.Year;
    private decimal _newBudgetAmount;
    private string _newBudgetDescription = string.Empty;
    private YearlyBudget? _selectedYearlyBudget;
    private decimal _unallocatedAmount;
    private decimal _allocationPercent;

    // ── Selected Office (for transactions sub-list) ───────────────────────────
    private OfficeAllocationItemViewModel? _selectedOffice;
    private string _transactionPanelHeader = string.Empty;

    public ObservableCollection<YearlyBudget> YearlyBudgets { get; } = new();
    public ObservableCollection<OfficeAllocationItemViewModel> DepartmentAllocations { get; } = new();
    public ObservableCollection<TblTransaction> OfficeTransactions { get; } = new();

    // ── Properties ───────────────────────────────────────────────────────────
    public int NewBudgetYear
    {
        get => _newBudgetYear;
        set { _newBudgetYear = value; OnPropertyChanged(); }
    }

    public decimal NewBudgetAmount
    {
        get => _newBudgetAmount;
        set { _newBudgetAmount = value; OnPropertyChanged(); CalculateUnallocated(); }
    }

    public string NewBudgetDescription
    {
        get => _newBudgetDescription;
        set { _newBudgetDescription = value; OnPropertyChanged(); }
    }

    public YearlyBudget? SelectedYearlyBudget
    {
        get => _selectedYearlyBudget;
        set
        {
            _selectedYearlyBudget = value;
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
    public ICommand AddYearlyBudgetCommand { get; }
    public ICommand SaveAllocationsCommand { get; }

    public BudgetAllocationViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();

        AddYearlyBudgetCommand = new RelayCommand(async _ => await AddYearlyBudgetAsync());
        SaveAllocationsCommand = new RelayCommand(async _ => await SaveAllocationsAsync(), _ => SelectedYearlyBudget != null);

        _ = LoadInitialDataAsync();
    }

    // ── Data Loading ─────────────────────────────────────────────────────────
    private async Task LoadInitialDataAsync()
    {
        var budgets = await _context.YearlyBudgets.OrderByDescending(b => b.Year).ToListAsync();
        YearlyBudgets.Clear();
        foreach (var b in budgets) YearlyBudgets.Add(b);
        if (YearlyBudgets.Any()) SelectedYearlyBudget = YearlyBudgets[0];
    }

    private async Task LoadAllocationsAsync()
    {
        DepartmentAllocations.Clear();
        OfficeTransactions.Clear();
        TransactionPanelHeader = string.Empty;

        if (SelectedYearlyBudget == null)
        {
            UnallocatedAmount = 0;
            return;
        }

        // Load offices + existing allocations
        var offices = await _context.Offices.OrderBy(o => o.OfficeCode).ToListAsync();
        var existingAllocations = await _context.OfficeAllocations
            .Where(a => a.YearlyBudgetId == SelectedYearlyBudget.Id)
            .ToListAsync();

        // Calculate SpentAmount per office from tbl_transaction
        var officeIds = offices.Select(o => o.Id).ToList();
        var spentByOffice = await _context.TblTransactions
            .Where(t => t.OfficeId.HasValue && officeIds.Contains(t.OfficeId.Value))
            .GroupBy(t => t.OfficeId!.Value)
            .Select(g => new { OfficeId = g.Key, Spent = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.OfficeId, x => x.Spent);

        foreach (var office in offices)
        {
            if (string.IsNullOrEmpty(office.OfficeCode)) continue;

            var allocation = existingAllocations.FirstOrDefault(a => a.OfficeCode == office.OfficeCode)
                             ?? new OfficeAllocation { OfficeCode = office.OfficeCode, YearlyBudgetId = SelectedYearlyBudget.Id };

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
    }

    private void CalculateUnallocated()
    {
        if (SelectedYearlyBudget == null) return;
        decimal totalAllocated = DepartmentAllocations.Sum(a => a.Amount);
        UnallocatedAmount = SelectedYearlyBudget.TotalAmount - totalAllocated;
        AllocationPercent = SelectedYearlyBudget.TotalAmount > 0
            ? (totalAllocated / SelectedYearlyBudget.TotalAmount) * 100
            : 0;
    }

    // ── Add Yearly Budget ─────────────────────────────────────────────────────
    private async Task AddYearlyBudgetAsync()
    {
        if (NewBudgetAmount <= 0) return;

        var existing = await _context.YearlyBudgets.FirstOrDefaultAsync(b => b.Year == NewBudgetYear);
        if (existing != null)
        {
            MessageBox.Show($"Budget for {NewBudgetYear} already exists.");
            return;
        }

        var budget = new YearlyBudget
        {
            Year = NewBudgetYear,
            TotalAmount = NewBudgetAmount,
            Description = NewBudgetDescription
        };

        _context.YearlyBudgets.Add(budget);
        await _context.SaveChangesAsync();

        YearlyBudgets.Insert(0, budget);
        SelectedYearlyBudget = budget;
        NewBudgetAmount = 0;
        NewBudgetDescription = string.Empty;
    }

    // ── Save Allocations ──────────────────────────────────────────────────────
    private async Task SaveAllocationsAsync()
    {
        if (UnallocatedAmount < 0)
        {
            MessageBox.Show("Total allocations cannot exceed the yearly budget.");
            return;
        }

        foreach (var vm in DepartmentAllocations)
        {
            if (vm.Model.Id == 0 && vm.Amount > 0)
            {
                vm.Model.AllocatedAmount = vm.Amount;
                _context.OfficeAllocations.Add(vm.Model);
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
}

// ── Office Allocation Item ViewModel ─────────────────────────────────────────
public class OfficeAllocationItemViewModel : ViewModelBase
{
    public OfficeAllocation Model { get; }
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

    public OfficeAllocationItemViewModel(OfficeAllocation model, string officeName, string? officeCode, decimal spentAmount = 0)
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
    public DepartmentAllocationViewModel(OfficeAllocation model, string officeName, string? officeCode)
        : base(model, officeName, officeCode) { }
}
