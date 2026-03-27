using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using GoodGovernanceApp.Views;

namespace GoodGovernanceApp.ViewModels;

public class DepartmentManagementViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private Office? _selectedOffice;
    private string _newDepartmentName = string.Empty;
    private string _newDepartmentDescription = string.Empty;
    private string _newOfficeCode = string.Empty;
    private string _newRoleName = string.Empty;
    private string _newRoleDescription = string.Empty;
    private ProjectDetail? _selectedRole;

    // XAML binds to "Departments" — keep the name for compatibility
    public ObservableCollection<Office> Departments { get; } = new();
    public ObservableCollection<DepartmentRole> SelectedDepartmentRoles { get; } = new();
    public ObservableCollection<ProjectDetail> ProjectDetails { get; } = new();

    public ProjectDetail? SelectedRole
    {
        get => _selectedRole;
        set { _selectedRole = value; OnPropertyChanged(); }
    }

    public Office? SelectedDepartment
    {
        get => _selectedOffice;
        set
        {
            _selectedOffice = value;
            OnPropertyChanged();
            _ = LoadRolesAsync();
        }
    }

    public string NewDepartmentName
    {
        get => _newDepartmentName;
        set 
        { 
            _newDepartmentName = value; 
            OnPropertyChanged();
            // Auto-generate a code whenever the name changes (if none yet)
            if (string.IsNullOrWhiteSpace(NewOfficeCode))
                NewOfficeCode = GenerateOfficeCode();
        }
    }

    /// <summary>Auto-generated unique Office ID shown in the Add form.</summary>
    public string NewOfficeCode
    {
        get => _newOfficeCode;
        set { _newOfficeCode = value; OnPropertyChanged(); }
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
    public ICommand RegenerateCodeCommand { get; }
    public ICommand AddProjectCommand { get; }

    public DepartmentManagementViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
        AddDepartmentCommand = new RelayCommand(async _ => await AddOfficeAsync());
        AddRoleCommand = new RelayCommand(async _ => await AddRoleAsync(), _ => SelectedDepartment != null);
        DeleteDepartmentCommand = new RelayCommand(async _ => await DeleteOfficeAsync(), _ => SelectedDepartment != null);
        RegenerateCodeCommand = new RelayCommand(_ => NewOfficeCode = GenerateOfficeCode());
        AddProjectCommand = new RelayCommand(_ => OpenAddProjectWindow(), _ => SelectedDepartment != null);

        _ = LoadOfficesAsync();
    }

    private async Task LoadOfficesAsync()
    {
        var offices = await _context.Offices.ToListAsync();
        Departments.Clear();
        foreach (var o in offices) Departments.Add(o);
    }

    private async Task LoadRolesAsync()
    {
        SelectedDepartmentRoles.Clear();
        ProjectDetails.Clear();

        if (SelectedDepartment != null)
        {
            System.Diagnostics.Debug.WriteLine($"=== Selected Office ===");
            System.Diagnostics.Debug.WriteLine($"Name: {SelectedDepartment.Name}");
            System.Diagnostics.Debug.WriteLine($"OfficeCode: '{SelectedDepartment.OfficeCode}'");

            var allProjects = await _context.ProjectDetails.ToListAsync();
            System.Diagnostics.Debug.WriteLine($"=== All Projects in DB ({allProjects.Count}) ===");
            foreach (var p in allProjects)
                System.Diagnostics.Debug.WriteLine($"  Project: {p.Name} | OfficeCode: '{p.OfficeCode}'");

            var filtered = allProjects
                .Where(p => p.OfficeCode == SelectedDepartment.OfficeCode)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"=== Filtered: {filtered.Count} matches ===");

            foreach (var project in filtered) ProjectDetails.Add(project);
        }
    }

    private string GenerateOfficeCode()
    {
        int year = DateTime.Now.Year;
        // Find the highest sequence number used this year
        int nextSeq = 1;
        var existing = Departments
            .Where(o => o.OfficeCode != null && o.OfficeCode.StartsWith($"OFF-{year}-"))
            .Select(o =>
            {
                var parts = o.OfficeCode!.Split('-');
                return parts.Length == 3 && int.TryParse(parts[2], out int n) ? n : 0;
            })
            .ToList();

        if (existing.Count > 0)
            nextSeq = existing.Max() + 1;

        return $"OFF-{year}-{nextSeq:D4}";
    }

    private async Task AddOfficeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDepartmentName)) return;

        // Ensure code is generated even if name was pasted without triggering setter
        if (string.IsNullOrWhiteSpace(NewOfficeCode))
            NewOfficeCode = GenerateOfficeCode();

        var office = new Office
        {
            Name        = NewDepartmentName,
            Description = NewDepartmentDescription,
            OfficeCode  = NewOfficeCode,
            CreatedAt   = DateTime.Now,
            UpdatedAt   = DateTime.Now
        };

        _context.Offices.Add(office);
        await _context.SaveChangesAsync();

        Departments.Add(office);
        NewDepartmentName = string.Empty;
        NewDepartmentDescription = string.Empty;
        NewOfficeCode = string.Empty;
    }

    private async Task AddRoleAsync()
    {
        if (SelectedDepartment == null || string.IsNullOrWhiteSpace(NewRoleName)) return;

        var role = new DepartmentRole
        {
            Name = NewRoleName,
            Description = NewRoleDescription,
            OfficeId = SelectedDepartment.Id
        };

        _context.DepartmentRoles.Add(role);
        await _context.SaveChangesAsync();
        
        SelectedDepartmentRoles.Add(role);
        NewRoleName = string.Empty;
        NewRoleDescription = string.Empty;
    }

    private async Task DeleteOfficeAsync()
    {
        if (SelectedDepartment == null) return;
        
        _context.Offices.Remove(SelectedDepartment);
        await _context.SaveChangesAsync();
        
        Departments.Remove(SelectedDepartment);
        SelectedDepartment = null;
    }

    private void OpenAddProjectWindow()
    {
        var window = new AddProjectWindow
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        bool? result = window.ShowDialog();

        if (result == true)
        {
            // Refresh the project list for the currently selected department
            _ = LoadRolesAsync();
        }
    }
}
