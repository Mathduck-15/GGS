using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.ViewModels;

public class NavigationItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "Apps"; // Default material design icon
    public string ViewToken { get; set; } = string.Empty;
}

public class MainViewModel : ViewModelBase
{
    private readonly Services.SessionService _sessionService;
    private NavigationItem _selectedNavItem = null!;
    private object _currentView = null!;

    public string CurrentUserName => _sessionService.CurrentUser?.Name ?? "Guest";
    public string CurrentUserRole => _sessionService.CurrentUser?.Role ?? "None";

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public NavigationItem SelectedNavItem
    {
        get => _selectedNavItem;
        set
        {
            _selectedNavItem = value;
            OnPropertyChanged();
            NavigateTo(value?.ViewToken);
        }
    }

    public object CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    public ICommand LogoutCommand { get; }

    public MainViewModel()
    {
        _sessionService = App.AppHost!.Services.GetRequiredService<Services.SessionService>();
        
        var allItems = new List<NavigationItem>
        {
            new NavigationItem { Name = "Dashboard", Icon = "ViewDashboard", ViewToken = "Dashboard" },
            new NavigationItem { Name = "My Profile", Icon = "AccountEdit", ViewToken = "Profile" },
            new NavigationItem { Name = "Users", Icon = "AccountGroup", ViewToken = "Users" },
            new NavigationItem { Name = "Parameters", Icon = "CogBox", ViewToken = "Parameters" },
            new NavigationItem { Name = "Budget & Transactions", Icon = "Finance", ViewToken = "Transactions" },
            new NavigationItem { Name = "Budget Allocation", Icon = "ScaleBalance", ViewToken = "BudgetAllocation" },
            new NavigationItem { Name = "Reports", Icon = "FileChart", ViewToken = "Reports" },
            new NavigationItem { Name = "Departments", Icon = "OfficeBuilding", ViewToken = "Departments" },
            new NavigationItem { Name = "File Center", Icon = "CloudUpload", ViewToken = "FileUpload" },
            new NavigationItem { Name = "Evaluation Center", Icon = "FileCertificate", ViewToken = "Evaluation" },
            new NavigationItem { Name = "Settings & Backups", Icon = "DatabaseSettings", ViewToken = "Settings" }
        };

        var role = _sessionService.CurrentUser?.Role;
        IEnumerable<NavigationItem> filtered;

        if (role == "SuperAdmin" || role == "Admin")
        {
            filtered = allItems;
        }
        else if (role == "Evaluator")
        {
            filtered = allItems.Where(i => i.ViewToken == "Dashboard" || i.ViewToken == "Profile" || i.ViewToken == "Evaluation");
        }
        else // Standard User
        {
            filtered = allItems.Where(i => i.ViewToken == "Dashboard" || i.ViewToken == "Profile" || i.ViewToken == "Transactions" || i.ViewToken == "FileUpload");
        }

        NavigationItems = new ObservableCollection<NavigationItem>(filtered);

        LogoutCommand = new RelayCommand(ExecuteLogout);

        // Set default view
        if (NavigationItems.Any())
            SelectedNavItem = NavigationItems[0];
    }

    private void NavigateTo(string? viewToken)
    {
        switch (viewToken)
        {
            case "Dashboard":
                CurrentView = new Views.DashboardView();
                break;
            case "Profile":
                CurrentView = new Views.ProfileView();
                break;
            case "Users":
                CurrentView = new Views.UserManagementView();
                break;
            case "Parameters":
                CurrentView = new Views.ParametersView();
                break;
            case "Transactions":
                CurrentView = new Views.BudgetTransactionsView();
                break;
            case "Reports":
                CurrentView = new Views.ReportsView();
                break;
            case "BudgetAllocation":
                CurrentView = new Views.BudgetAllocationView();
                break;
            case "Settings":
                CurrentView = new Views.SettingsView();
                break;
            case "Departments":
                CurrentView = new Views.DepartmentManagementView();
                break;
            case "FileUpload":
                CurrentView = new Views.FileUploadView();
                break;
            case "Evaluation":
                CurrentView = new Views.EvaluationView();
                break;
            default:
                // Show a placeholder or dashboard text block
                CurrentView = new System.Windows.Controls.TextBlock 
                { 
                    Text = viewToken + " View Placeholder", 
                    FontSize = 24, 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center, 
                    VerticalAlignment = System.Windows.VerticalAlignment.Center 
                };
                break;
        }
    }

    private void ExecuteLogout(object? parameter)
    {
        // Re-open login window and close main window
        if (parameter is System.Windows.Window window)
        {
            _sessionService.ClearSession();
            var loginWindow = App.AppHost!.Services.GetService(typeof(Views.LoginWindow)) as Views.LoginWindow;
            loginWindow!.Show();
            window.Close();
        }
    }
}
