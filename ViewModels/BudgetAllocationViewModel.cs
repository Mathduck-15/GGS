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
    
    // Yearly Budget Form
    private int _newBudgetYear = DateTime.Now.Year;
    private decimal _newBudgetAmount;
    private string _newBudgetDescription = string.Empty;
    private YearlyBudget? _selectedYearlyBudget;

    // Allocation Logic
    private decimal _unallocatedAmount;
    private decimal _allocationPercent;

    public ObservableCollection<YearlyBudget> YearlyBudgets { get; } = new();
    public ObservableCollection<OfficeAllocationItemViewModel> DepartmentAllocations { get; } = new();

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

    public ICommand AddYearlyBudgetCommand { get; }
    public ICommand SaveAllocationsCommand { get; }

    public BudgetAllocationViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
        
        AddYearlyBudgetCommand = new RelayCommand(async _ => await AddYearlyBudgetAsync());
        SaveAllocationsCommand = new RelayCommand(async _ => await SaveAllocationsAsync(), _ => SelectedYearlyBudget != null);

        _ = LoadInitialDataAsync();
    }

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
        if (SelectedYearlyBudget == null)
        {
            UnallocatedAmount = 0;
            return;
        }

        var offices = await _context.Offices.ToListAsync();
        var existingAllocations = await _context.OfficeAllocations
            .Where(a => a.YearlyBudgetId == SelectedYearlyBudget.Id)
            .ToListAsync();

        foreach (var office in offices)
        {
            if (string.IsNullOrEmpty(office.OfficeCode)) continue;

            var allocation = existingAllocations.FirstOrDefault(a => a.OfficeCode == office.OfficeCode) 
                             ?? new OfficeAllocation { OfficeCode = office.OfficeCode, YearlyBudgetId = SelectedYearlyBudget.Id };
            
            var vm = new OfficeAllocationItemViewModel(allocation, office.Name, office.OfficeCode);
            vm.AmountChanged += (s, e) => CalculateUnallocated();
            DepartmentAllocations.Add(vm);
        }

        CalculateUnallocated();
    }

    private void CalculateUnallocated()
    {
        if (SelectedYearlyBudget == null) return;

        decimal totalAllocated = DepartmentAllocations.Sum(a => a.Amount);
        UnallocatedAmount = SelectedYearlyBudget.TotalAmount - totalAllocated;
        
        if (SelectedYearlyBudget.TotalAmount > 0)
            AllocationPercent = (totalAllocated / SelectedYearlyBudget.TotalAmount) * 100;
        else
            AllocationPercent = 0;
    }

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
        MessageBox.Show("Allocations saved successfully.");
    }
}

// Renamed from DepartmentAllocationViewModel to OfficeAllocationItemViewModel
public class OfficeAllocationItemViewModel : ViewModelBase
{
    public OfficeAllocation Model { get; }
    public string DepartmentName { get; }
    public string? OfficeCode { get; }
    private decimal _amount;

    public decimal Amount
    {
        get => _amount;
        set
        {
            _amount = value;
            OnPropertyChanged();
            AmountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? AmountChanged;

    public OfficeAllocationItemViewModel(OfficeAllocation model, string officeName, string? officeCode)
    {
        Model = model;
        DepartmentName = officeName;
        OfficeCode = officeCode;
        _amount = model.AllocatedAmount;
    }
}

// Keep old name as alias for XAML compatibility
public class DepartmentAllocationViewModel : OfficeAllocationItemViewModel
{
    public DepartmentAllocationViewModel(OfficeAllocation model, string officeName, string? officeCode) : base(model, officeName, officeCode) { }
}
