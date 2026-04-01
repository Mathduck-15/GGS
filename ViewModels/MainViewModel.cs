using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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


    private string _governanceName = "Good Governance Management System";


    private string? _profilePhotoPath;
    private BitmapImage? _profilePhotoSource;


    public BitmapImage? ProfilePhotoSource
    {
        get => _profilePhotoSource;
        set
        {
            _profilePhotoSource = value;
            OnPropertyChanged();
        }
    }

    public string GovernanceName
    {
        get => _governanceName;
        set { _governanceName = value; OnPropertyChanged(); }
    }

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
    public ICommand OpenAppProfileCommand { get; }

    public MainViewModel()
    {
        _sessionService = App.AppHost!.Services.GetRequiredService<Services.SessionService>();
        OpenAppProfileCommand = new RelayCommand(ExecuteOpenAppProfile);
        LoadApplicationProfileAsync();
        LoadProfilePhotoAsync();

        var allItems = new List<NavigationItem>
        {
            new NavigationItem { Name = "Dashboard", Icon = "ViewDashboard", ViewToken = "Dashboard" },
            new NavigationItem { Name = "My Profile", Icon = "AccountEdit", ViewToken = "Profile" },
            new NavigationItem { Name = "Users", Icon = "AccountGroup", ViewToken = "Users" },
            new NavigationItem { Name = "Parameters", Icon = "CogBox", ViewToken = "Parameters" },
            new NavigationItem { Name = "Transactions", Icon = "Finance", ViewToken = "Transactions" },
            new NavigationItem { Name = "Budget Allocation", Icon = "ScaleBalance", ViewToken = "BudgetAllocation" },
            new NavigationItem { Name = "CRS Beneficiaries", Icon = "AccountMultiple", ViewToken = "CrsBeneficiary" },
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

    private async System.Threading.Tasks.Task LoadProfilePhotoAsync()
    {
        try
        {
            var dbHelper = App.AppHost!.Services.GetRequiredService<GoodGovernanceApp.Data.DatabaseHelper>();
            string query = $"SELECT profile_photo FROM users WHERE Id = {_sessionService.CurrentUser?.Id};";
            var dataTable = await dbHelper.ExecuteQueryAsync(query);

            if (dataTable.Rows.Count > 0)
            {
                var path = dataTable.Rows[0]["profile_photo"]?.ToString();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // allows file to be released so you can upload a new one
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    ProfilePhotoSource = bitmap;
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Could not load profile photo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async System.Threading.Tasks.Task LoadApplicationProfileAsync()
    {
        try
        {
            var dbHelper = App.AppHost!.Services.GetRequiredService<GoodGovernanceApp.Data.DatabaseHelper>();
            string query = "SELECT GoveName, Address FROM goveprofile LIMIT 1;";
            var dataTable = await dbHelper.ExecuteQueryAsync(query);

            if (dataTable.Rows.Count > 0)
            {
                var row = dataTable.Rows[0];

                string goveName = row["GoveName"]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(goveName))
                    GovernanceName = goveName;
            }
        }
        catch { }
    }

    public void NavigateTo(string? viewToken, object? parameter = null)
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
                var allocationView = new Views.BudgetAllocationView();
                if (parameter is string officeCode && allocationView.DataContext is BudgetAllocationViewModel vm)
                {
                    vm.ActivateForOffice(officeCode);
                }
                CurrentView = allocationView;
                break;
            case "CrsBeneficiary":
                CurrentView = new Views.CrsBeneficiaryView();
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
        _sessionService.ClearSession();
        
        var loginWindow = App.AppHost!.Services.GetService(typeof(Views.LoginWindow)) as Views.LoginWindow;
        loginWindow!.Show();

        // The CommandParameter binding often fails inside a DrawerHost across Visual Trees in WPF.
        // We find the main window safely using Application.Current to close it.
        var window = parameter as System.Windows.Window 
                     ?? System.Windows.Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault();
        
        window?.Close();
    }

    private void ExecuteOpenAppProfile(object? parameter)
    {
        var window = new Views.ApplicationProfileWindow();
        window.ShowDialog();
    }
}
