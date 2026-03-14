using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;

namespace GoodGovernanceApp.ViewModels;

public class UserManagementViewModel : ViewModelBase
{
    private readonly AppDbContext _context;

    // ── Users ─────────────────────────────────────────────────────────────────
    private ObservableCollection<User> _users = new();
    private User _selectedUser = new();
    private string _searchText = string.Empty;
    private ICollectionView _usersView;
    private bool _isEditing;
    private ObservableCollection<Office> _offices = new();
    private ObservableCollection<DepartmentRole> _allRoles = new();
    private ObservableCollection<DepartmentRole> _filteredRoles = new();

    // ── ValidateUsers ────────────────────────────────────────────────────────
    private ObservableCollection<ValidateUser> _validateUsers = new();
    private ValidateUser? _selectedValidateUser;
    private ICollectionView? _validateUsersView;
    private bool _filterAll = true;
    private bool _filterPending;
    private bool _filterAccepted;
    private bool _filterRejected;

    // ── Users Properties ─────────────────────────────────────────────────────
    public ObservableCollection<User> Users
    {
        get => _users;
        set { _users = value; OnPropertyChanged(); }
    }

    public ObservableCollection<Office> Offices
    {
        get => _offices;
        set { _offices = value; OnPropertyChanged(); }
    }

    public ObservableCollection<DepartmentRole> OfficeRoles
    {
        get => _filteredRoles;
        set { _filteredRoles = value; OnPropertyChanged(); }
    }

    public User SelectedUser
    {
        get => _selectedUser;
        set
        {
            _selectedUser = value ?? new User();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedOfficeId));
            RefreshFilteredRoles();
        }
    }

    public long? SelectedOfficeId
    {
        get => SelectedUser.OfficeId;
        set
        {
            SelectedUser.OfficeId = value;
            OnPropertyChanged();
            RefreshFilteredRoles();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            _usersView?.Refresh();
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotEditing));
        }
    }

    public bool IsNotEditing => !IsEditing;
    public ObservableCollection<string> Roles { get; } = new() { "SuperAdmin", "Admin", "Evaluator", "User" };

    // ── ValidateUsers Properties ─────────────────────────────────────────────
    public ObservableCollection<ValidateUser> ValidateUsers
    {
        get => _validateUsers;
        set { _validateUsers = value; OnPropertyChanged(); }
    }

    public ICollectionView? ValidateUsersView
    {
        get => _validateUsersView;
        set { _validateUsersView = value; OnPropertyChanged(); }
    }

    public ValidateUser? SelectedValidateUser
    {
        get => _selectedValidateUser;
        set { _selectedValidateUser = value; OnPropertyChanged(); }
    }

    public bool FilterAll
    {
        get => _filterAll;
        set { _filterAll = value; OnPropertyChanged(); _validateUsersView?.Refresh(); }
    }

    public bool FilterPending
    {
        get => _filterPending;
        set { _filterPending = value; OnPropertyChanged(); _validateUsersView?.Refresh(); }
    }

    public bool FilterAccepted
    {
        get => _filterAccepted;
        set { _filterAccepted = value; OnPropertyChanged(); _validateUsersView?.Refresh(); }
    }

    public bool FilterRejected
    {
        get => _filterRejected;
        set { _filterRejected = value; OnPropertyChanged(); _validateUsersView?.Refresh(); }
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RefreshValidateUsersCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public UserManagementViewModel()
    {
        try
        {
            _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
            LoadData();
        }
        catch (Exception ex)
        {


            _context = null!;
        }

        _usersView = CollectionViewSource.GetDefaultView(Users);
        _usersView.Filter = FilterUsers;

        AddCommand    = new RelayCommand(ExecuteAdd);
        EditCommand   = new RelayCommand(ExecuteEdit, CanExecuteEdit);
        SaveCommand   = new RelayCommand(ExecuteSave, CanExecuteSave);
        DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
        CancelCommand = new RelayCommand(ExecuteCancel);
        RefreshValidateUsersCommand = new RelayCommand(_ => LoadValidateUsers());
    }

    // ── Data Loading ──────────────────────────────────────────────────────────
    private void LoadData()
    {
        try
        {
            _context.Users
                .Include(u => u.Office)
                .Include(u => u.ValidationInfo)
                .Load();

            Users.Clear();
            foreach (var user in _context.Users.Local)
                Users.Add(user);

            _usersView = CollectionViewSource.GetDefaultView(Users);
            _usersView.Filter = FilterUsers;

            var offcs = _context.Offices.OrderBy(d => d.Name).ToList();
            Offices.Clear();
            foreach (var d in offcs) Offices.Add(d);

            var roles = _context.DepartmentRoles.OrderBy(r => r.Name).ToList();
            _allRoles.Clear();
            foreach (var r in roles) _allRoles.Add(r);

            RefreshFilteredRoles();
            LoadValidateUsers();
        }
         catch 
        {

        }
    }

    private void LoadValidateUsers()
    {
        try
        {
            var list = _context.ValidateUsers
                .Include(v => v.Category)
                .Include(v => v.AppUser)
                .OrderByDescending(v => v.CreatedAt)
                .ToList();

            ValidateUsers.Clear();
            foreach (var v in list)
                ValidateUsers.Add(v);

            var view = CollectionViewSource.GetDefaultView(ValidateUsers);
            view.Filter = FilterValidateUsers;
            ValidateUsersView = view;
        }
        catch { }
    }

    // ── Filters ───────────────────────────────────────────────────────────────
    private bool FilterUsers(object obj)
    {
        if (obj is User user)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            return user.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   user.Role.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private bool FilterValidateUsers(object obj)
    {
        if (obj is not ValidateUser v) return false;
        if (FilterAll)      return true;
        if (FilterPending)  return v.Status == "pending";
        if (FilterAccepted) return v.Status == "accepted";
        if (FilterRejected) return v.Status == "rejected";
        return true;
    }

    private void RefreshFilteredRoles()
    {
        _filteredRoles.Clear();
        if (SelectedUser?.OfficeId != null)
        {
            var filtered = _allRoles.Where(r => r.OfficeId == SelectedUser.OfficeId).ToList();
            foreach (var r in filtered) _filteredRoles.Add(r);
        }
        OnPropertyChanged(nameof(OfficeRoles));
    }

    // ── User Commands ─────────────────────────────────────────────────────────
    private void ExecuteAdd(object? parameter)
    {
        SelectedUser = new User();
        IsEditing = true;
    }

    private bool CanExecuteEdit(object? parameter) => SelectedUser.Id != 0 && !IsEditing;

    private void ExecuteEdit(object? parameter) => IsEditing = true;

    private bool CanExecuteSave(object? parameter)
        => IsEditing && !string.IsNullOrWhiteSpace(SelectedUser.Name) && !string.IsNullOrWhiteSpace(SelectedUser.Role);

    private void ExecuteSave(object? parameter)
    {
        if (SelectedUser.Id == 0)
        {
            if (string.IsNullOrEmpty(SelectedUser.Password))
                SelectedUser.Password = "defaultpass";
            SelectedUser.Password = PasswordHasher.HashPassword(SelectedUser.Password);
            _context.Users.Add(SelectedUser);
        }
        else if (_context.Entry(SelectedUser).Property(u => u.Password).IsModified)
        {
            SelectedUser.Password = PasswordHasher.HashPassword(SelectedUser.Password);
        }

        _context.SaveChanges();
        IsEditing = false;
        LoadData();
    }

    private bool CanExecuteDelete(object? parameter) => SelectedUser.Id != 0 && !IsEditing;

    private void ExecuteDelete(object? parameter)
    {
        if (SelectedUser.Id != 0)
        {
            _context.Users.Remove(SelectedUser);
            _context.SaveChanges();
            SelectedUser = new User();
            LoadData();
        }
    }

    private void ExecuteCancel(object? parameter)
    {
        IsEditing = false;
        SelectedUser = new User();
        foreach (var entry in _context.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged))
            entry.State = EntityState.Unchanged;
    }
}
