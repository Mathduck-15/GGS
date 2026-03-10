using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.ViewModels;

public class DepartmentManagementViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private Department? _selectedDepartment;
    private string _newDepartmentName = string.Empty;
    private string _newDepartmentDescription = string.Empty;
    private string _newRoleName = string.Empty;
    private string _newRoleDescription = string.Empty;

    public ObservableCollection<Department> Departments { get; } = new();
    public ObservableCollection<DepartmentRole> SelectedDepartmentRoles { get; } = new();

    public Department? SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            _selectedDepartment = value;
            OnPropertyChanged();
            _ = LoadRolesAsync();
        }
    }

    public string NewDepartmentName
    {
        get => _newDepartmentName;
        set { _newDepartmentName = value; OnPropertyChanged(); }
    }

    public string NewDepartmentDescription
    {
        get => _newDepartmentDescription;
        set { _newDepartmentDescription = value; OnPropertyChanged(); }
    }

    public string NewRoleName
    {
        get => _newRoleName;
        set { _newRoleName = value; OnPropertyChanged(); }
    }

    public string NewRoleDescription
    {
        get => _newRoleDescription;
        set { _newRoleDescription = value; OnPropertyChanged(); }
    }

    public ICommand AddDepartmentCommand { get; }
    public ICommand AddRoleCommand { get; }
    public ICommand DeleteDepartmentCommand { get; }

    public DepartmentManagementViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
        AddDepartmentCommand = new RelayCommand(async _ => await AddDepartmentAsync());
        AddRoleCommand = new RelayCommand(async _ => await AddRoleAsync(), _ => SelectedDepartment != null);
        DeleteDepartmentCommand = new RelayCommand(async _ => await DeleteDepartmentAsync(), _ => SelectedDepartment != null);

        _ = LoadDepartmentsAsync();
    }

    private async Task LoadDepartmentsAsync()
    {
        var departments = await _context.Departments.ToListAsync();
        Departments.Clear();
        foreach (var dept in departments)
        {
            Departments.Add(dept);
        }
    }

    private async Task LoadRolesAsync()
    {
        SelectedDepartmentRoles.Clear();
        if (SelectedDepartment != null)
        {
            var roles = await _context.DepartmentRoles
                .Where(r => r.DepartmentId == SelectedDepartment.Id)
                .ToListAsync();
            foreach (var role in roles)
            {
                SelectedDepartmentRoles.Add(role);
            }
        }
    }

    private async Task AddDepartmentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDepartmentName)) return;

        var dept = new Department
        {
            Name = NewDepartmentName,
            Description = NewDepartmentDescription
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();
        
        Departments.Add(dept);
        NewDepartmentName = string.Empty;
        NewDepartmentDescription = string.Empty;
    }

    private async Task AddRoleAsync()
    {
        if (SelectedDepartment == null || string.IsNullOrWhiteSpace(NewRoleName)) return;

        var role = new DepartmentRole
        {
            Name = NewRoleName,
            Description = NewRoleDescription,
            DepartmentId = SelectedDepartment.Id
        };

        _context.DepartmentRoles.Add(role);
        await _context.SaveChangesAsync();
        
        SelectedDepartmentRoles.Add(role);
        NewRoleName = string.Empty;
        NewRoleDescription = string.Empty;
    }

    private async Task DeleteDepartmentAsync()
    {
        if (SelectedDepartment == null) return;
        
        _context.Departments.Remove(SelectedDepartment);
        await _context.SaveChangesAsync();
        
        Departments.Remove(SelectedDepartment);
        SelectedDepartment = null;
    }
}
