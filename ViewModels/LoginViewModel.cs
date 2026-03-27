using System.Windows;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Views;
using GoodGovernanceApp.Utilities;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private string _governanceName = "Good Governance Management System";
    private System.Windows.Media.Imaging.BitmapImage? _logoSource;
    private readonly GoodGovernanceApp.Services.SessionService _sessionService;
    private string _address = string.Empty;

    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(); }
    }

    public string GovernanceName
    {
        get => _governanceName;
        set { _governanceName = value; OnPropertyChanged(); }
    }

    public System.Windows.Media.Imaging.BitmapImage? LogoSource
    {
        get => _logoSource;
        set { _logoSource = value; OnPropertyChanged(); }
    }


    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public ICommand LoginCommand { get; }
    public ICommand OpenDbSettingsCommand { get; }
    public ICommand CheatCommand { get; }

    public LoginViewModel(GoodGovernanceApp.Services.SessionService sessionService)
    {
        _sessionService = sessionService;
        LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        OpenDbSettingsCommand = new RelayCommand(ExecuteOpenDbSettings);
        CheatCommand = new RelayCommand(p => MessageBox.Show("username = admin and password = admin", "password = admin", MessageBoxButton.OK, MessageBoxImage.Information));  

        _ = LoadApplicationProfileAsync();
    }

    private async System.Threading.Tasks.Task LoadApplicationProfileAsync()
    {
        try
        {
            var dbHelper = App.AppHost!.Services.GetRequiredService<GoodGovernanceApp.Data.DatabaseHelper>();
            string query = "SELECT GoveName, LogoAddress, Address FROM goveprofile LIMIT 1;";
            var dataTable = await dbHelper.ExecuteQueryAsync(query);

            if (dataTable.Rows.Count > 0)
            {
                var row = dataTable.Rows[0];
                string goveName = row["GoveName"]?.ToString() ?? "";
                string logoAddress = row["LogoAddress"]?.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(goveName))
                {
                    GovernanceName = goveName;
                }

                if (!string.IsNullOrWhiteSpace(logoAddress) && System.IO.File.Exists(logoAddress))
                {
                    var bi = new System.Windows.Media.Imaging.BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bi.UriSource = new System.Uri(logoAddress, System.UriKind.Absolute);
                    bi.EndInit();
                    LogoSource = bi;
                }

                string address = row["Address"]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(address))
                    Address = address;

            }
        }
        catch
        {
            // Ignore failure, fallbacks are handled by initial values and XAML
        }
    }

    private void ExecuteOpenDbSettings(object? parameter)
    {
        var settingsWindow = new DatabaseSettingsWindow();
        settingsWindow.ShowDialog();
    }

    private bool CanExecuteLogin(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }

    private void ExecuteLogin(object? parameter)
    {
        string hashedInput = PasswordHasher.HashPassword(Password);
        
        using var scope = App.AppHost!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = context.Users.FirstOrDefault(u => u.Name == Username && u.Password == hashedInput);
        
        if (user != null || (Username == "admin" && Password == "admin")) // admin override
        {
            if (user == null && Username == "admin")
            {
                // Ensure we have a dummy admin in session if using override
                user = new Models.User { Name = "admin", Role = "SuperAdmin" };
            }

            _sessionService.CurrentUser = user;

            var mainWindow = App.AppHost!.Services.GetService(typeof(MainWindow)) as MainWindow;
            mainWindow!.Show();
            
            // Close login window
            if (parameter is Window window)
            {
                window.Close();
            }
        }
        else
        {
            ErrorMessage = "Invalid Username or Password";
        }
    }
}
