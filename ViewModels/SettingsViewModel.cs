using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using GoodGovernanceApp.Services;

namespace GoodGovernanceApp.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly BackupService _backupService;
    private readonly string _appSettingsPath = "appsettings.json";

    private bool _useRemoteDatabase;
    public bool UseRemoteDatabase
    {
        get => _useRemoteDatabase;
        set { _useRemoteDatabase = value; OnPropertyChanged(); }
    }

    private string _localConnectionString = string.Empty;
    public string LocalConnectionString
    {
        get => _localConnectionString;
        set { _localConnectionString = value; OnPropertyChanged(); }
    }

    private string _remoteConnectionString = string.Empty;
    public string RemoteConnectionString
    {
        get => _remoteConnectionString;
        set { _remoteConnectionString = value; OnPropertyChanged(); }
    }

    private string _mySqlDumpPath = "mysqldump";
    public string MySqlDumpPath
    {
        get => _mySqlDumpPath;
        set { _mySqlDumpPath = value; OnPropertyChanged(); }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand SaveSettingsCommand { get; }
    public ICommand FullBackupCommand { get; }
    public ICommand DifferentialBackupCommand { get; }
    public ICommand IncrementalBackupCommand { get; }

    public SettingsViewModel()
    {
        _backupService = new BackupService();
        LoadSettings();

        SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
        FullBackupCommand = new RelayCommand(async p => await ExecuteBackup("Full"));
        DifferentialBackupCommand = new RelayCommand(async p => await ExecuteBackup("Differential"));
        IncrementalBackupCommand = new RelayCommand(async p => await ExecuteBackup("Incremental"));
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_appSettingsPath))
            {
                string json = File.ReadAllText(_appSettingsPath);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("AppSettings", out var appSettings) && appSettings.TryGetProperty("UseRemoteDatabase", out var useRemote))
                {
                    UseRemoteDatabase = useRemote.GetBoolean();
                }

                if (root.TryGetProperty("ConnectionStrings", out var connStrings))
                {
                    if (connStrings.TryGetProperty("LocalConnection", out var localConn))
                        LocalConnectionString = localConn.GetString() ?? "";
                    
                    if (connStrings.TryGetProperty("RemoteConnection", out var remoteConn))
                        RemoteConnectionString = remoteConn.GetString() ?? "";
                }

                if (root.TryGetProperty("AppSettings", out var appSettingsProp) && appSettingsProp.TryGetProperty("MySqlDumpPath", out var dumpPath))
                {
                    MySqlDumpPath = dumpPath.GetString() ?? "mysqldump";
                }
            }
        }
        catch { StatusMessage = "Error loading settings."; }
    }

    private void ExecuteSaveSettings(object? parameter)
    {
        try
        {
            var config = new
            {
                ConnectionStrings = new
                {
                    LocalConnection = LocalConnectionString,
                    RemoteConnection = RemoteConnectionString
                },
                AppSettings = new
                {
                    UseRemoteDatabase = UseRemoteDatabase,
                    MySqlDumpPath = MySqlDumpPath
                }
            };

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_appSettingsPath, json);
            
            StatusMessage = "Settings saved. Restart application to apply connection changes.";
        }
        catch { StatusMessage = "Error saving settings."; }
    }

    private async Task ExecuteBackup(string type)
    {
        StatusMessage = $"Starting {type} backup...";
        string backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        string connStr = UseRemoteDatabase ? RemoteConnectionString : LocalConnectionString;

        _backupService.MySqlDumpPath = MySqlDumpPath;

        bool success = false;
        switch (type)
        {
            case "Full":
                success = await _backupService.CreateFullBackupAsync(connStr, backupDir);
                break;
            case "Differential":
                success = await _backupService.CreateDifferentialBackupAsync(connStr, backupDir);
                break;
            case "Incremental":
                success = await _backupService.CreateIncrementalBackupAsync(connStr, backupDir);
                break;
        }

        StatusMessage = success ? $"{type} backup completed successfully in ./Backups/" : $"Failed to create {type} backup. Check if mysqldump is installed.";
    }
}
