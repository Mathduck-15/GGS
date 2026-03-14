using System.Windows;
using System.Windows.Input;
using GoodGovernanceApp.Data;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Views;
using GoodGovernanceApp.Utilities;
using System.Linq;

namespace GoodGovernanceApp.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private readonly AppDbContext _context;
    private readonly GoodGovernanceApp.Services.SessionService _sessionService;

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

    public LoginViewModel(AppDbContext context, GoodGovernanceApp.Services.SessionService sessionService)
    {
        _context = context;
        _sessionService = sessionService;
        LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
        OpenDbSettingsCommand = new RelayCommand(ExecuteOpenDbSettings);
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
        
        var user = _context.Users.FirstOrDefault(u => u.Name == Username && u.Password == hashedInput);
        
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
