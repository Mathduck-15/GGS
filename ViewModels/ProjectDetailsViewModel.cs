using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.ViewModels;

public class ProjectDetailsViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private List<ProjectDetail> _allProjects = new();
    private Office? _selectedDepartment;
    private ProjectDetail? _selectedRole;

    public ObservableCollection<ProjectDetail> SelectedDepartmentRoles { get; } = new();

    public Office? SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            _selectedDepartment = value;
            OnPropertyChanged();
            FilterByOfficeCode(value?.OfficeCode);
        }
    }

    public ProjectDetail? SelectedRole
    {
        get => _selectedRole;
        set
        {
            _selectedRole = value;
            OnPropertyChanged();
        }
    }

    public ProjectDetailsViewModel()
    {
        _context = App.AppHost!.Services.GetRequiredService<AppDbContext>();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _allProjects = await _context.ProjectDetails.ToListAsync();
        FilterByOfficeCode(_selectedDepartment?.OfficeCode);
    }

    private void FilterByOfficeCode(string? officeCode)
    {
        SelectedDepartmentRoles.Clear();

        var filtered = string.IsNullOrEmpty(officeCode)
            ? _allProjects
            : _allProjects.Where(p => p.OfficeCode == officeCode).ToList();

        foreach (var project in filtered)
            SelectedDepartmentRoles.Add(project);
    }
}