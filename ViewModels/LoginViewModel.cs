using GoodGovernanceApp.Data;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Utilities;
using GoodGovernanceApp.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GoodGovernanceApp.ViewModels;

public class LoginViewModel : ViewModelBase
{
    // ── Fields ───────────────────────────────────────────────────────────────
    private string _username     = string.Empty;
    private string _password     = string.Empty;
    private string _errorMessage = string.Empty;
    private bool   _isLoggingIn  = false;
    private string _governanceName = "Good Governance Management System";
    private System.Windows.Media.Imaging.BitmapImage? _logoSource;
    private string _address = string.Empty;
    private readonly GoodGovernanceApp.Services.SessionService _sessionService;
    private BitmapImage? _systemPhotoSource;
    public ICommand CheatCommand { get; }

    // ── Properties ───────────────────────────────────────────────────────────
    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); ClearError(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); ClearError(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    /// <summary>True while the async login is in progress (disables the button).</summary>
    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set { _isLoggingIn = value; OnPropertyChanged(); }
    }

    private void ExecuteCheat()
    {
        MessageBox.Show("Username: superadmin\nPassword: password", "Cheat Codes", MessageBoxButton.OK, MessageBoxImage.Information);
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

    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(); }
    }

    // ── Commands ─────────────────────────────────────────────────────────────
    public ICommand LoginCommand       { get; }
    public ICommand OpenDbSettingsCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public LoginViewModel(GoodGovernanceApp.Services.SessionService sessionService)
    {
        _sessionService = sessionService;

        LoginCommand          = new RelayCommand(async p => await ExecuteLoginAsync(p), CanExecuteLogin);
        OpenDbSettingsCommand = new RelayCommand(_ => new DatabaseSettingsWindow().ShowDialog());

        _ = LoadApplicationProfileAsync();

        LoadSystemPhotoAsync();
        CheatCommand = new RelayCommand(_ => ExecuteCheat());
    }

    // ── CanExecute ────────────────────────────────────────────────────────────
    private bool CanExecuteLogin(object? _)
        => !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !IsLoggingIn;

    private void ClearError() => ErrorMessage = string.Empty;

    // ── Login Logic ───────────────────────────────────────────────────────────
    /// <summary>
    /// Authenticates the user against the `users` table.
    ///
    /// Lookup strategy:
    ///   1. Try matching the typed username against the `name` column (display name).
    ///   2. If not found, retry with the `email` column — accommodates users who
    ///      log in with their email address.
    ///
    /// Password verification:
    ///   The stored hash must be a SHA-256 hex digest (produced by PasswordHasher.HashPassword).
    ///   ⚠ If passwords were seeded via Laravel's bcrypt(), the comparison will always fail.
    ///     In that case install BCrypt.Net-Next (NuGet) and use BCrypt.Verify() instead.
    ///
    /// Status check:
    ///   Users with status = "inactive" or "suspended" are rejected with a clear message.
    /// </summary>
    private async Task ExecuteLoginAsync(object? parameter)
    {
        IsLoggingIn  = true;
        ErrorMessage = string.Empty;

        try
        {
            User? user = null;

            // ── 1. Try to find user in the database ──────────────────────────
            using var scope   = App.AppHost!.Services.CreateScope();
            var context       = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string inputLower = Username.Trim().ToLowerInvariant();

            // First try: match by `name` (case-insensitive)
            user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Name.ToLower() == inputLower);

            // Second try: match by `email` — handles users who type their email
            if (user == null)
            {
                user = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.Email.ToLower() == inputLower);
            }

            // ── 2. Validate existence ────────────────────────────────────────
            if (user == null)
            {
                ErrorMessage = "❌ No account found with that username or email.";
                return;
            }

            // ── 3. Validate password ─────────────────────────────────────────
            bool passwordOk = PasswordHasher.VerifyPassword(Password, user.Password);

            if (!passwordOk)
            {
                ErrorMessage = "❌ Incorrect password. Please try again.";
                return;
            }

            // ── 4. Check account status ──────────────────────────────────────
            if (user.Status?.Equals("inactive", StringComparison.OrdinalIgnoreCase) == true)
            {
                ErrorMessage = "⚠ Your account is inactive. Please contact an administrator.";
                return;
            }

            if (user.Status?.Equals("suspended", StringComparison.OrdinalIgnoreCase) == true)
            {
                ErrorMessage = "🚫 Your account has been suspended. Please contact an administrator.";
                return;
            }

            // ── 5. Login success ─────────────────────────────────────────────
            _sessionService.CurrentUser = user;

            var mainWindow = App.AppHost!.Services.GetService(typeof(MainWindow)) as MainWindow;
            mainWindow!.Show();

            if (parameter is Window window)
                window.Close();
        }
        catch (Exception ex)
        {
            // Surface a user-friendly message; the raw exception helps diagnose
            // connection or column-mapping issues during development.
            ErrorMessage = $"❌ Login error: {ex.Message}";
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    // ── Application Profile ───────────────────────────────────────────────────
    private async Task LoadApplicationProfileAsync()
    {
        try
        {
            var dbHelper = App.AppHost!.Services.GetRequiredService<DatabaseHelper>();
            string query = "SELECT GoveName, LogoAddress, Address FROM goveprofile LIMIT 1;";
            var dataTable = await dbHelper.ExecuteQueryAsync(query);

            if (dataTable.Rows.Count > 0)
            {
                var row        = dataTable.Rows[0];
                string govName = row["GoveName"]?.ToString() ?? "";
                string logoUrl = row["LogoAddress"]?.ToString() ?? "";
                string addr    = row["Address"]?.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(govName))
                    GovernanceName = govName;

                if (!string.IsNullOrWhiteSpace(addr))
                    Address = addr;

                if (!string.IsNullOrWhiteSpace(logoUrl) && System.IO.File.Exists(logoUrl))
                {
                    var bi = new System.Windows.Media.Imaging.BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bi.UriSource   = new Uri(logoUrl, UriKind.Absolute);
                    bi.EndInit();
                    LogoSource = bi;
                }
            }
        }
        catch
        {
            // Non-fatal — fallback values apply via initial property values.
        }
    }

    public BitmapImage? SystemPhotoSource
    {
        get => _systemPhotoSource;
        set { _systemPhotoSource = value; OnPropertyChanged(); }
    }



    private async System.Threading.Tasks.Task LoadSystemPhotoAsync()
    {
        try
        {
            var dbHelper = App.AppHost!.Services.GetRequiredService<GoodGovernanceApp.Data.DatabaseHelper>();

            // Ensure table exists first to avoid Table doesn't exist exception
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS systemsprofile (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    PhotoAddress NVARCHAR(500)
                );";
            await dbHelper.ExecuteNonQueryAsync(createTableQuery);

            string query = "SELECT PhotoAddress FROM systemsprofile LIMIT 1;";
            var dataTable = await dbHelper.ExecuteQueryAsync(query);

            if (dataTable.Rows.Count > 0)
            {
                var path = dataTable.Rows[0]["PhotoAddress"]?.ToString();

                // Resolve relative path if needed
                if (!string.IsNullOrWhiteSpace(path) && !System.IO.Path.IsPathRooted(path))
                    path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

                if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                        bitmap.EndInit();
                        SystemPhotoSource = bitmap;
                    });
                }
            }
        }
        catch { }
    }
}
