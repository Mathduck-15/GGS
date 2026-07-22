using GoodGovernanceApp.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Org.BouncyCastle.Utilities.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GoodGovernanceApp.ViewModels;

/// <summary>
/// Represents a Metro tile navigation item.
/// </summary>
public class NavigationItem
{
    public string Name      { get; set; } = string.Empty;
    public string Icon      { get; set; } = "Apps";
    public string ViewToken { get; set; } = string.Empty;

    /// <summary>Hex color string for the tile background, e.g. "#FF009688"</summary>
    public string TileColor { get; set; } = "#FF009688";

    /// <summary>Optional tile group label shown in the dashboard.</summary>
    public string Group     { get; set; } = string.Empty;
}

public class AppNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public System.DateTime Date { get; set; }
    public string ViewToken { get; set; } = string.Empty;
    public string Icon { get; set; } = "Bell";
    public string Color { get; set; } = "#3B82F6";
    public string FormattedDate => Date.ToString("MMM dd, hh:mm tt");
}

public class MainViewModel : ViewModelBase
{
    // ── services ─────────────────────────────────────────────────────────────
    private readonly Services.SessionService _sessionService;
    private readonly Timer _clockTimer;

    // ── backing fields ────────────────────────────────────────────────────────
    private NavigationItem _selectedNavItem = null!;
    private object         _currentView     = null!;
    private bool           _isShowingDashboard = true;
    private bool           _isSidebarOpen   = false;
    private string         _governanceName  = "Good Governance Management System";
    private string         _currentDate     = string.Empty;
    private bool           _isDatabaseConnected = true;
    private string?        _profilePhotoPath;
    private BitmapImage?   _profilePhotoSource;
    private string         _currentSectionTitle = "Home";
    private BitmapImage?   _systemPhotoSource;
    private BitmapImage? _systemGovPhotoSource;
    private BitmapImage?   _copyrightPhotoSource;
    private bool           _hasNewNotifications = false;

    // ── public properties ─────────────────────────────────────────────────────
    public string CurrentUserName => _sessionService.CurrentUser?.Name ?? "Guest";
    public string CurrentUserRole => _sessionService.CurrentUser?.Role ?? "None";

    public string CurrentDate
    {
        get => _currentDate;
        private set { _currentDate = value; OnPropertyChanged(); }
    }

    public bool IsDatabaseConnected
    {
        get => _isDatabaseConnected;
        set { _isDatabaseConnected = value; OnPropertyChanged(); }
    }

    public bool IsShowingDashboard
    {
        get => _isShowingDashboard;
        set
        {
            _isShowingDashboard = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsShowingView));
        }
    }

    /// <summary>Inverse of IsShowingDashboard – used in XAML visibility bindings.</summary>
    public bool IsShowingView => !_isShowingDashboard;

    public bool IsSidebarOpen
    {
        get => _isSidebarOpen;
        set { _isSidebarOpen = value; OnPropertyChanged(); }
    }

    public string CurrentSectionTitle
    {
        get => _currentSectionTitle;
        set { _currentSectionTitle = value; OnPropertyChanged(); }
    }

    public BitmapImage? ProfilePhotoSource
    {
        get => _profilePhotoSource;
        set { _profilePhotoSource = value; OnPropertyChanged(); }
    }

    public BitmapImage? SystemPhotoSource
    {
        get => _systemPhotoSource;
        set { _systemPhotoSource = value; OnPropertyChanged(); }
    }

    public BitmapImage? GovPhotoSource
    {
        get => _systemGovPhotoSource;
        set { _systemGovPhotoSource = value; OnPropertyChanged(); }
    }

    public BitmapImage? CopyrightPhotoSource
    {
        get => _copyrightPhotoSource;
        set { _copyrightPhotoSource = value; OnPropertyChanged(); }
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
        }
    }

    public object CurrentView
    {
        get => _currentView;
        set { _currentView = value; OnPropertyChanged(); }
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }
    public ObservableCollection<AppNotification> Notifications { get; } = new();

    public bool HasNewNotifications
    {
        get => _hasNewNotifications;
        set { _hasNewNotifications = value; OnPropertyChanged(); }
    }

    private bool _isNotificationPopupOpen;
    public bool IsNotificationPopupOpen
    {
        get => _isNotificationPopupOpen;
        set { _isNotificationPopupOpen = value; OnPropertyChanged(); }
    }

    private bool _isOnline = false;
    public bool IsOnline
    {
        get => _isOnline;
        set { _isOnline = value; OnPropertyChanged(); }
    }

    private bool _isSyncing = false;
    public bool IsSyncing
    {
        get => _isSyncing;
        set { _isSyncing = value; OnPropertyChanged(); }
    }

    private string _syncStatus = "Waiting...";
    public string SyncStatus
    {
        get => _syncStatus;
        set { _syncStatus = value; OnPropertyChanged(); }
    }

    // ── commands ──────────────────────────────────────────────────────────────
    public ICommand LogoutCommand        { get; }
    public ICommand OpenAppProfileCommand{ get; }
    public ICommand OpenSystemsProfileCommand { get; }
    public ICommand NavigateTileCommand  { get; }
    public ICommand ShowDashboardCommand { get; }
    public ICommand ToggleSidebarCommand { get; }
    public ICommand NotificationClickedCommand { get; }
    public ICommand OpenNotificationsCommand   { get; }
    public ICommand SyncNowCommand             { get; }

    // ── constructor ───────────────────────────────────────────────────────────
    public MainViewModel()
    {
        _sessionService = App.AppHost!.Services.GetRequiredService<Services.SessionService>();

        // Live clock
        CurrentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy  •  hh:mm tt");
        _clockTimer = new Timer(_ =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
                CurrentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy  •  hh:mm tt"));
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));

        // Sync Events
        GoodGovernanceApp.Services.ConnectivityService.OnConnectionStatusChanged += (isOnline) =>
        {
            Application.Current?.Dispatcher.Invoke(() => IsOnline = isOnline);
        };
        GoodGovernanceApp.Services.SyncService.OnSyncStatusChanged += (isSyncing) =>
        {
            Application.Current?.Dispatcher.Invoke(() => IsSyncing = isSyncing);
        };
        GoodGovernanceApp.Services.SyncService.OnSyncProgress += (msg) =>
        {
            Application.Current?.Dispatcher.Invoke(() => SyncStatus = msg);
        };

        // Seed the initial state from whatever ConnectivityService already knows
        IsOnline   = GoodGovernanceApp.Services.ConnectivityService.IsOnline;
        SyncStatus = "Checking...";

        // Ask ConnectivityService to push its current status to all subscribers immediately
        GoodGovernanceApp.Services.ConnectivityService.SyncCurrentStatus();

        // Commands
        SyncNowCommand = new RelayCommand(async _ => await GoodGovernanceApp.Services.SyncService.SyncNowAsync(), _ => IsOnline && !IsSyncing);
        OpenAppProfileCommand = new RelayCommand(ExecuteOpenAppProfile);
        OpenSystemsProfileCommand = new RelayCommand(ExecuteOpenSystemsProfile);
        LogoutCommand         = new RelayCommand(ExecuteLogout);
        NavigateTileCommand   = new RelayCommand(ExecuteNavigateTile);
        ShowDashboardCommand  = new RelayCommand(_ =>
        {
            IsShowingDashboard   = true;
            CurrentSectionTitle  = "Home";
            CurrentView          = null!;
        });
        ToggleSidebarCommand  = new RelayCommand(_ => IsSidebarOpen = !IsSidebarOpen);
        NotificationClickedCommand  = new RelayCommand(ExecuteNotificationClicked);
        OpenNotificationsCommand    = new RelayCommand(_ => IsNotificationPopupOpen = true);

        LoadApplicationProfileAsync();
        LoadProfilePhotoAsync();
        LoadSystemPhotoAsync();
        LoadGovProfileAsync();
        LoadCopyrightPhotoAsync();
        LoadNotificationsAsync();

        // ── Build navigation items with Metro tile colors ──────────────────
        var allItems = new List<NavigationItem>
        {
            new() { Name="Dashboard",        Icon="ViewDashboard",    ViewToken="Dashboard",      TileColor="#D90000", Group="Main"       },
            new() { Name="My Profile",       Icon="AccountEdit",      ViewToken="Profile",        TileColor="#8DB355", Group="Main"       },
            new() { Name="Users",            Icon="AccountGroup",     ViewToken="Users",          TileColor="#D90000", Group="Management" },
            new() { Name="Parameters",       Icon="CogBox",           ViewToken="Parameters",     TileColor="#8DB355", Group="System"     },
            new() { Name="Transactions",     Icon="Finance",          ViewToken="Transactions",   TileColor="#D90000", Group="Finance"    },
            new() { Name="Consolidated",     Icon="TableMultiple",    ViewToken="ConsolidatedTransactions", TileColor="#8DB355", Group="Finance" },
            new() { Name="Budget Allocation",Icon="ScaleBalance",     ViewToken="BudgetAllocation",TileColor="#D90000",Group="Finance"   },
            new() { Name="CRS Beneficiaries",Icon="AccountMultiple",  ViewToken="CrsBeneficiary", TileColor="#8DB355", Group="Management" },
            new() { Name="Reports",          Icon="FileChart",        ViewToken="Reports",        TileColor="#D90000", Group="Reports"    },

            new() { Name="File Center",      Icon="CloudUpload",      ViewToken="FileUpload",     TileColor="#8DB355", Group="System"     },
            new() { Name="Evaluation Center",Icon="FileCertificate",  ViewToken="Evaluation",     TileColor="#D90000", Group="Reports"    },
            new() { Name="Audit Log",        Icon="FormatListBulleted",ViewToken="AuditLog",      TileColor="#8DB355", Group="System"     },
            new() { Name="Settings & Backups",Icon="DatabaseSettings",ViewToken="Settings",       TileColor="#D90000", Group="System"     },
        };

        var role = _sessionService.CurrentUser?.Role;
        IEnumerable<NavigationItem> filtered;

        if (role == "SuperAdmin" || role == "Admin")
        {
            filtered = allItems;
        }
        else if (role == "Evaluator")
        {
            filtered = allItems.Where(i => i.ViewToken is "Dashboard" or "Profile" or "Evaluation");
        }
        else // Standard User
        {
            filtered = allItems.Where(i => i.ViewToken is "Dashboard" or "Profile" or "Transactions" or "ConsolidatedTransactions" or "FileUpload");
        }

        NavigationItems = new ObservableCollection<NavigationItem>(filtered);

        // Start on the dashboard tile grid
        IsShowingDashboard  = true;
        CurrentSectionTitle = "Home";
    }

    // ── tile navigation ───────────────────────────────────────────────────────
    private void ExecuteNavigateTile(object? parameter)
    {
        if (parameter is not string viewToken) return;

        if (viewToken == "BudgetAllocation")
        {
            var dialog = new Views.BudgetYearSelectionWindow();
            var result = dialog.ShowDialog();
            if (result == true && dialog.DataContext is BudgetYearSelectionViewModel vm && vm.SelectedMasterBudget != null)
            {
                NavigateTo("BudgetAllocation", vm.SelectedMasterBudget);
                CurrentSectionTitle = $"Budget Allocation - {vm.SelectedMasterBudget.FiscalYear}";
                IsShowingDashboard = false;
            }
            return;
        }

        if (viewToken == "ConsolidatedTransactions")
        {
            var searchDialog = new Views.ConsolidatedSearchWindow();
            searchDialog.Owner = Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault();
            var searchResult = searchDialog.ShowDialog();
            if (searchResult == true)
            {
                var searchParam = (Mode: searchDialog.SearchMode, Value: searchDialog.SearchValue);
                NavigateTo("ConsolidatedTransactions", searchParam);
                CurrentSectionTitle = searchDialog.SearchMode switch
                {
                    "BeneficiaryId" => $"Consolidated – ID: {searchDialog.SearchValue}",
                    "FullName"      => $"Consolidated – Name: {searchDialog.SearchValue}",
                    "ProjectCode"   => $"Consolidated – Project: {searchDialog.SearchValue}",
                    "OfficeCode"    => $"Consolidated – Office: {searchDialog.SearchValue}",
                    "Barangay"      => $"Consolidated – Brgy: {searchDialog.SearchValue}",
                    "HouseholdNo"   => $"Consolidated – Household: {searchDialog.SearchValue}",
                    _               => "Consolidated Transactions"
                };
                IsShowingDashboard = false;
            }
            return;
        }

        NavigateTo(viewToken);
        CurrentSectionTitle = NavigationItems.FirstOrDefault(i => i.ViewToken == viewToken)?.Name ?? viewToken;
        IsShowingDashboard  = false;
    }

    private void ExecuteNotificationClicked(object? parameter)
    {
        if (parameter is string viewToken)
        {
            ExecuteNavigateTile(viewToken);
            HasNewNotifications = false;
            IsNotificationPopupOpen = false;
        }
    }

    // ── public navigation (called by other ViewModels too) ────────────────────
    public void NavigateTo(string? viewToken, object? parameter = null)
    {
        switch (viewToken)
        {
            case "Dashboard":
                CurrentView = new Views.DashboardView();
                break;
            case "Home":
                IsShowingDashboard  = true;
                CurrentSectionTitle = "Home";
                CurrentView         = null!;
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
            case "ConsolidatedTransactions":
                var consolidatedView = new Views.ConsolidatedTransactionsView();
                if (parameter is (string mode, string value) &&
                    consolidatedView.DataContext is ConsolidatedTransactionsPageViewModel ctVm)
                {
                    ctVm.ApplyInitialSearch(mode, value);
                }
                CurrentView = consolidatedView;
                break;
            case "Reports":
                CurrentView = new Views.ReportsView();
                break;
            case "BudgetAllocation":
                var allocationView = new Views.BudgetAllocationView();
                if (allocationView.DataContext is BudgetAllocationViewModel allocVm)
                {
                    if (parameter is Models.MasterBudget selectedBudget)
                    {
                        allocVm.InitializeWithBudget(selectedBudget);
                    }
                    else if (parameter is string officeCode)
                    {
                        allocVm.ActivateForOffice(officeCode);
                    }
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
            case "AuditLog":
                CurrentView = new Views.AuditLogView();
                break;
            default:
                CurrentView = new System.Windows.Controls.TextBlock
                {
                    Text = viewToken + " – coming soon",
                    FontSize = 24,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                };
                break;
        }
    }

    // ── profile photo ─────────────────────────────────────────────────────────
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
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource   = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    ProfilePhotoSource = bitmap;
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not load profile photo: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                var goveName = dataTable.Rows[0]["GoveName"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(goveName))
                    GovernanceName = goveName;
            }
        }
        catch { }
    }

    // ── system photo ──────────────────────────────────────────────────────────
    private async System.Threading.Tasks.Task LoadSystemPhotoAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var imagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                if (System.IO.Directory.Exists(imagesDir))
                {
                    var file = System.IO.Directory.EnumerateFiles(imagesDir, "*profile*.*").FirstOrDefault();
                    if (file == null) 
                    {
                        file = System.IO.Directory.EnumerateFiles(imagesDir, "*system*.*").FirstOrDefault();
                    }

                    if (file != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(file, UriKind.Absolute);
                            bitmap.EndInit();
                            SystemPhotoSource = bitmap;
                        });
                    }
                }
            });
        }
        catch { }
    }

    // ── copyright photo ──────────────────────────────────────────────────────
    private async System.Threading.Tasks.Task LoadCopyrightPhotoAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var imagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                if (System.IO.Directory.Exists(imagesDir))
                {
                    var file = System.IO.Directory.EnumerateFiles(imagesDir, "*copyright*.*").FirstOrDefault();
                    if (file != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(file, UriKind.Absolute);
                            bitmap.EndInit();
                            CopyrightPhotoSource = bitmap;
                        });
                    }
                }
            });
        }
        catch { }
    }

    private async Task LoadGovProfileAsync()
    {
        try
        {
            var dbHelper = App.AppHost!.Services.GetRequiredService<DatabaseHelper>();

            // ✅ ADD THIS — create table first before querying
            string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS goveprofile (
                id INT AUTO_INCREMENT PRIMARY KEY,
                GoveName NVARCHAR(255),
                Address NVARCHAR(255),
                LogoAddress NVARCHAR(500)
            );";
            await dbHelper.ExecuteNonQueryAsync(createTableQuery);

            string query = "SELECT GoveName, LogoAddress, Address FROM goveprofile LIMIT 1;";
            var dataTable = await dbHelper.ExecuteQueryAsync(query);

            if (dataTable.Rows.Count > 0)
            {
                var row = dataTable.Rows[0];
                string govName = row["GoveName"]?.ToString() ?? "";
                string addr = row["Address"]?.ToString() ?? "";
                // Note: We ignore LogoAddress from DB now.
            }

            // Load logo from Assets/Images instead
            await Task.Run(() =>
            {
                var imagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                if (System.IO.Directory.Exists(imagesDir))
                {
                    var file = System.IO.Directory.EnumerateFiles(imagesDir, "*logo*.*").FirstOrDefault();
                    if (file == null) 
                    {
                        file = System.IO.Directory.EnumerateFiles(imagesDir, "*gov*.*").FirstOrDefault();
                    }

                    if (file != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var bi = new BitmapImage();
                            bi.BeginInit();
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.UriSource = new Uri(file, UriKind.Absolute);
                            bi.EndInit();
                            GovPhotoSource = bi;
                        });
                    }
                }
            });
        }
        catch (Exception ex) // ✅ CHANGE THIS TOO — never silently swallow errors
        {
            System.Diagnostics.Debug.WriteLine($"[MethodName] Error: {ex.Message}");
        }
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            var dbHelper = App.AppHost!.Services.GetRequiredService<GoodGovernanceApp.Data.DatabaseHelper>();
            var notifications = new List<AppNotification>();

            // Query TblTransaction (Budget Transactions)
            string budgetQuery = "SELECT id, amount, description, created_at FROM tbl_transaction ORDER BY created_at DESC LIMIT 5;";
            var budgetData = await dbHelper.ExecuteQueryAsync(budgetQuery);
            foreach (System.Data.DataRow row in budgetData.Rows)
            {
                if (row["created_at"] is DateTime date)
                {
                    notifications.Add(new AppNotification
                    {
                        Title = "New Budget Transaction",
                        Message = $"₱{Convert.ToDecimal(row["amount"]):N2} - {row["description"]}",
                        Date = date,
                        ViewToken = "Transactions",
                        Icon = "Finance",
                        Color = "#EAB308"
                    });
                }
            }

            // Query ConsolidatedTransactions
            string consolidatedQuery = "SELECT id, amount, transaction_type, created_at FROM consolidated_transactions ORDER BY created_at DESC LIMIT 5;";
            var consolidatedData = await dbHelper.ExecuteQueryAsync(consolidatedQuery);
            foreach (System.Data.DataRow row in consolidatedData.Rows)
            {
                if (row["created_at"] is DateTime date)
                {
                    notifications.Add(new AppNotification
                    {
                        Title = "Consolidated Record Added",
                        Message = $"₱{Convert.ToDecimal(row["amount"]):N2} - {row["transaction_type"]}",
                        Date = date,
                        ViewToken = "ConsolidatedTransactions",
                        Icon = "TableMultiple",
                        Color = "#5E35B1"
                    });
                }
            }

            var sortedNotifications = notifications.OrderByDescending(n => n.Date).Take(10).ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Notifications.Clear();
                foreach (var n in sortedNotifications)
                {
                    Notifications.Add(n);
                }
                
                if (Notifications.Any())
                {
                    HasNewNotifications = true;
                    // Automatically open the popup to notify the user
                    IsNotificationPopupOpen = true;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadNotificationsAsync Error: {ex.Message}");
        }
    }

    // ── logout ────────────────────────────────────────────────────────────────
    private void ExecuteLogout(object? parameter)
    {
        _clockTimer.Dispose();
        _sessionService.ClearSession();

        var loginWindow = App.AppHost!.Services.GetService(typeof(Views.LoginWindow)) as Views.LoginWindow;
        loginWindow!.Show();

        var window = parameter as System.Windows.Window
                     ?? Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault();
        window?.Close();
    }

    // ── app profile window ────────────────────────────────────────────────────
    private void ExecuteOpenAppProfile(object? parameter)
    {
        var window = new Views.ApplicationProfileWindow();
        window.ShowDialog();
    }

    private void ExecuteOpenSystemsProfile(object? parameter)
    {
        var window = new Views.SystemsApplicationProfile();
        window.ShowDialog();
    }
}
