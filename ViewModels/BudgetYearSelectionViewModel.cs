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

public class BudgetYearSelectionViewModel : ViewModelBase
{
    private readonly AppDbContext _context;

    private int _newBudgetYear = DateTime.Now.Year;
    private decimal _newBudgetAmount;
    private string _newBudgetDescription = string.Empty;
    private YearlyBudget? _selectedYearlyBudget;

    public ObservableCollection<YearlyBudget> YearlyBudgets { get; } = new();

    public int NewBudgetYear
    {
        get => _newBudgetYear;
        set { _newBudgetYear = value; OnPropertyChanged(); }
    }

    public decimal NewBudgetAmount
    {
        get => _newBudgetAmount;
        set { _newBudgetAmount = value; OnPropertyChanged(); }
    }

    public string NewBudgetDescription
    {
        get => _newBudgetDescription;
        set { _newBudgetDescription = value; OnPropertyChanged(); }
    }

    public YearlyBudget? SelectedYearlyBudget
    {
        get => _selectedYearlyBudget;
        set { _selectedYearlyBudget = value; OnPropertyChanged(); }
    }

    public ICommand AddYearlyBudgetCommand { get; }

    public BudgetYearSelectionViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();

        AddYearlyBudgetCommand = new RelayCommand(async _ => await AddYearlyBudgetAsync());

        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        var budgets = await _context.YearlyBudgets.OrderByDescending(b => b.Year).ToListAsync();
        YearlyBudgets.Clear();
        foreach (var b in budgets) YearlyBudgets.Add(b);
        if (YearlyBudgets.Any()) SelectedYearlyBudget = YearlyBudgets[0];
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
}
