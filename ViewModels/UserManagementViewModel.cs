using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace GoodGovernanceApp.ViewModels;

public class UserManagementViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private ObservableCollection<User> _users = new();
    private User _selectedUser = new();
    private string _searchText = string.Empty;
    private ICollectionView _usersView;
    private bool _isEditing;
    private ObservableCollection<Department> _departments = new();
    private ObservableCollection<DepartmentRole> _allRoles = new();
    private ObservableCollection<DepartmentRole> _filteredRoles = new();

    public ObservableCollection<User> Users
    {
        get => _users;
        set { _users = value; OnPropertyChanged(); }
    }

    public ObservableCollection<Department> Departments
    {
        get => _departments;
        set { _departments = value; OnPropertyChanged(); }
    }

    public ObservableCollection<DepartmentRole> DepartmentRoles
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
            OnPropertyChanged(nameof(SelectedDepartmentId)); // Add this
            RefreshFilteredRoles();
        }
    }

    public int? SelectedDepartmentId
    {
        get => SelectedUser.DepartmentId;
        set
        {
            SelectedUser.DepartmentId = value;
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

    public ICommand AddCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CancelCommand { get; }

    public ObservableCollection<string> Roles { get; } = new() { "SuperAdmin", "Admin", "Evaluator", "User" };

    public UserManagementViewModel()
    {
        // For WPF Design Time
        try
        {
             _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
             LoadData();
        }
        catch
        {
            // Ignore in design mode
        }

        _usersView = CollectionViewSource.GetDefaultView(Users);
        _usersView.Filter = FilterUsers;

        AddCommand = new RelayCommand(ExecuteAdd);
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    private void LoadData()
    {
        try
        {
            _context.Users
                .Include(u => u.Department)
                .Include(u => u.DepartmentRole)
                .Load();

            Users.Clear();
            foreach (var user in _context.Users.Local)
            {
                Users.Add(user);
            }

            _usersView = CollectionViewSource.GetDefaultView(Users);
            _usersView.Filter = FilterUsers;

            var depts = _context.Departments.OrderBy(d => d.Name).ToList();
            Departments.Clear();
            foreach (var d in depts) Departments.Add(d);

            var roles = _context.DepartmentRoles.OrderBy(r => r.Name).ToList();
            _allRoles.Clear();
            foreach (var r in roles) _allRoles.Add(r);

            RefreshFilteredRoles();
        }
        catch 
        {
            // Database might not be available yet
        }
    }

    private void RefreshFilteredRoles()
    {
        _filteredRoles.Clear();
        if (SelectedUser?.DepartmentId != null)
        {
            var filtered = _allRoles.Where(r => r.DepartmentId == SelectedUser.DepartmentId).ToList();
            foreach (var r in filtered) _filteredRoles.Add(r);
        }
        else
        {
            // If no department selected, maybe show all or none? Usually none or a hint.
            // Let's show none or all for safety if we want, but usually it's better to show none.
            // foreach (var r in _allRoles) _filteredRoles.Add(r); 
        }
        OnPropertyChanged(nameof(DepartmentRoles));
    }

    private bool FilterUsers(object obj)
    {
        if (obj is User user)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            return user.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   user.Role.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private void ExecuteAdd(object? parameter)
    {
        SelectedUser = new User();
        IsEditing = true;
    }

    private bool CanExecuteSave(object? parameter)
    {
        return IsEditing && !string.IsNullOrWhiteSpace(SelectedUser.Username) && !string.IsNullOrWhiteSpace(SelectedUser.Role);
    }

    private void ExecuteSave(object? parameter)
    {
        if (SelectedUser.Id == 0)
        {
            if (string.IsNullOrEmpty(SelectedUser.PasswordHash))
                SelectedUser.PasswordHash = "defaultpass"; // Requires actual hashing later
                
            _context.Users.Add(SelectedUser);
        }
        
        _context.SaveChanges();
        IsEditing = false;
        LoadData();
    }

    private bool CanExecuteDelete(object? parameter)
    {
        return SelectedUser.Id != 0 && !IsEditing;
    }

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
        // Discard unsaved changes locally
        foreach (var entry in _context.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged))
        {
            entry.State = EntityState.Unchanged;
        }
    }
}
