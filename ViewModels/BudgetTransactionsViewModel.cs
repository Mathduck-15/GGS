using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.ViewModels;

public class BudgetTransactionsViewModel : ViewModelBase
{
    private readonly AppDbContext _context;

    // Budgets
    private ObservableCollection<Budget> _budgets = new();
    private Budget _selectedBudget = new();
    private bool _isEditingBudget;

    // Transactions
    private ObservableCollection<Transaction> _transactions = new();
    private Transaction _selectedTransaction = new();
    private bool _isEditingTransaction;
    private string _transactionSearchText = string.Empty;
    private ICollectionView _transactionsView;

    // Lookups
    public ObservableCollection<Category> Categories { get; private set; } = new();
    public ObservableCollection<string> TransactionTypes { get; } = new() { "Income", "Expense" };

    // Properties
    public ObservableCollection<Budget> Budgets
    {
        get => _budgets;
        set { _budgets = value; OnPropertyChanged(); }
    }

    public Budget SelectedBudget
    {
        get => _selectedBudget;
        set { _selectedBudget = value ?? new Budget(); OnPropertyChanged(); }
    }

    public bool IsEditingBudget
    {
        get => _isEditingBudget;
        set { _isEditingBudget = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotEditingBudget)); }
    }
    public bool IsNotEditingBudget => !IsEditingBudget;

    public ObservableCollection<Transaction> Transactions
    {
        get => _transactions;
        set { _transactions = value; OnPropertyChanged(); }
    }

    public Transaction SelectedTransaction
    {
        get => _selectedTransaction;
        set { _selectedTransaction = value ?? new Transaction { Date = DateTime.Now }; OnPropertyChanged(); }
    }

    public bool IsEditingTransaction
    {
        get => _isEditingTransaction;
        set { _isEditingTransaction = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotEditingTransaction)); }
    }
    public bool IsNotEditingTransaction => !IsEditingTransaction;

    public string TransactionSearchText
    {
        get => _transactionSearchText;
        set
        {
            _transactionSearchText = value;
            OnPropertyChanged();
            _transactionsView?.Refresh();
        }
    }

    // Commands
    public ICommand AddBudgetCommand { get; }
    public ICommand SaveBudgetCommand { get; }
    public ICommand DeleteBudgetCommand { get; }
    public ICommand CancelBudgetCommand { get; }

    public ICommand AddTransactionCommand { get; }
    public ICommand SaveTransactionCommand { get; }
    public ICommand DeleteTransactionCommand { get; }
    public ICommand CancelTransactionCommand { get; }

    public BudgetTransactionsViewModel()
    {
        try
        {
            _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
            LoadData();
        }
        catch { }

        _transactionsView = CollectionViewSource.GetDefaultView(Transactions);
        _transactionsView.Filter = FilterTransactions;

        AddBudgetCommand = new RelayCommand(p => { SelectedBudget = new Budget(); IsEditingBudget = true; });
        CancelBudgetCommand = new RelayCommand(p => { CancelChanges(); IsEditingBudget = false; SelectedBudget = new Budget(); });
        SaveBudgetCommand = new RelayCommand(ExecuteSaveBudget, p => IsEditingBudget && SelectedBudget.CategoryId != 0 && SelectedBudget.Amount > 0);
        DeleteBudgetCommand = new RelayCommand(ExecuteDeleteBudget, p => SelectedBudget.Id != 0 && !IsEditingBudget);

        AddTransactionCommand = new RelayCommand(p => { SelectedTransaction = new Transaction { Date = DateTime.Now, TransactionType = "Expense" }; IsEditingTransaction = true; });
        CancelTransactionCommand = new RelayCommand(p => { CancelChanges(); IsEditingTransaction = false; SelectedTransaction = new Transaction(); });
        SaveTransactionCommand = new RelayCommand(ExecuteSaveTransaction, p => IsEditingTransaction && SelectedTransaction.CategoryId != 0 && SelectedTransaction.Amount > 0);
        DeleteTransactionCommand = new RelayCommand(ExecuteDeleteTransaction, p => SelectedTransaction.Id != 0 && !IsEditingTransaction);
    }

    private void LoadData()
    {
        try
        {
            _context.Categories.Load();
            Categories = _context.Categories.Local.ToObservableCollection();

            _context.Budgets.Include(b => b.Category).Load();
            Budgets = _context.Budgets.Local.ToObservableCollection();

            _context.Transactions.Include(t => t.Category).Include(t => t.User).Load();
            Transactions = _context.Transactions.Local.ToObservableCollection();
            
            _transactionsView = CollectionViewSource.GetDefaultView(Transactions);
            _transactionsView.Filter = FilterTransactions;
            
            OnPropertyChanged(nameof(Categories));
        }
        catch { }
    }

    private bool FilterTransactions(object obj)
    {
        if (obj is Transaction t)
        {
            if (string.IsNullOrWhiteSpace(TransactionSearchText)) return true;
            
            string search = TransactionSearchText.ToLower();
            bool matchCategory = t.Category?.Name.ToLower().Contains(search) ?? false;
            bool matchDesc = t.Description?.ToLower().Contains(search) ?? false;
            bool matchType = t.TransactionType.ToLower().Contains(search);
            
            return matchCategory || matchDesc || matchType;
        }
        return false;
    }

    private void ExecuteSaveBudget(object? obj)
    {
        if (SelectedBudget.Id == 0) _context.Budgets.Add(SelectedBudget);
        _context.SaveChanges();
        IsEditingBudget = false;
        LoadData();
    }

    private void ExecuteDeleteBudget(object? obj)
    {
        _context.Budgets.Remove(SelectedBudget);
        _context.SaveChanges();
        SelectedBudget = new Budget();
        LoadData();
    }

    private void ExecuteSaveTransaction(object? obj)
    {
        if (SelectedTransaction.Id == 0) _context.Transactions.Add(SelectedTransaction);
        _context.SaveChanges();
        IsEditingTransaction = false;
        LoadData();
    }

    private void ExecuteDeleteTransaction(object? obj)
    {
        _context.Transactions.Remove(SelectedTransaction);
        _context.SaveChanges();
        SelectedTransaction = new Transaction();
        LoadData();
    }

    private void CancelChanges()
    {
        foreach (var entry in _context.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged))
        {
            entry.State = EntityState.Unchanged;
        }
    }
}
