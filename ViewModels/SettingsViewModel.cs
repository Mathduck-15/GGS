using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GoodGovernanceApp.Models;
using GoodGovernanceApp.Services;
using GoodGovernanceApp.Utilities;
using Microsoft.Win32;
using MySqlConnector;

namespace GoodGovernanceApp.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly BackupService _backupService;
    private readonly BackupSchedulerService _scheduler;

    // ── Database Mode ────────────────────────────────────────────────────────
    private string _databaseMode = "Local";
    public string DatabaseMode
    {
        get => _databaseMode;
        set { _databaseMode = value; OnPropertyChanged(); }
    }

    private string _selectedPreset = string.Empty;
    public string SelectedPreset
    {
        get => _selectedPreset;
        set { _selectedPreset = value; OnPropertyChanged(); }
    }

    // ── GGMS Connection Strings ──────────────────────────────────────────────
    private string _localConnectionString = string.Empty;
    public string LocalConnectionString
    {
        get => _localConnectionString;
        set { _localConnectionString = value; OnPropertyChanged(); }
    }

    private string _lanConnectionString = string.Empty;
    public string LanConnectionString
    {
        get => _lanConnectionString;
        set { _lanConnectionString = value; OnPropertyChanged(); }
    }

    private string _remoteConnectionString = string.Empty;
    public string RemoteConnectionString
    {
        get => _remoteConnectionString;
        set { _remoteConnectionString = value; OnPropertyChanged(); }
    }

    // ── CRS Connection Fields ────────────────────────────────────────────────
    private string _crsServer = "localhost";
    public string CrsServer
    {
        get => _crsServer;
        set { _crsServer = value; OnPropertyChanged(); }
    }

    private string _crsPort = "3306";
    public string CrsPort
    {
        get => _crsPort;
        set { _crsPort = value; OnPropertyChanged(); }
    }

    private string _crsDatabase = "crs_db";
    public string CrsDatabase
    {
        get => _crsDatabase;
        set { _crsDatabase = value; OnPropertyChanged(); }
    }

    private string _crsUser = "root";
    public string CrsUser
    {
        get => _crsUser;
        set { _crsUser = value; OnPropertyChanged(); }
    }

    private string _crsPassword = string.Empty;
    public string CrsPassword
    {
        get => _crsPassword;
        set { _crsPassword = value; OnPropertyChanged(); }
    }

    // ── LAN IP ──────────────────────────────────────────────────────────────
    private string _lanIp = "192.168.1.1";
    public string LanIp
    {
        get => _lanIp;
        set
        {
            _lanIp = value;
            OnPropertyChanged();
            LanConnectionString = BuildGgmsConnStr(value, "governance");
            CrsServer = value;
        }
    }

    // ── Status ───────────────────────────────────────────────────────────────
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private string _ggmsTestResult = string.Empty;
    public string GgmsTestResult
    {
        get => _ggmsTestResult;
        set { _ggmsTestResult = value; OnPropertyChanged(); }
    }

    private string _crsTestResult = string.Empty;
    public string CrsTestResult
    {
        get => _crsTestResult;
        set { _crsTestResult = value; OnPropertyChanged(); }
    }

    private bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        set { _isTesting = value; OnPropertyChanged(); }
    }

    // ── Backup Settings ──────────────────────────────────────────────────────
    private string _backupType = "Full";
    /// <summary>Full | Differential | Incremental</summary>
    public string BackupType
    {
        get => _backupType;
        set { _backupType = value; OnPropertyChanged(); }
    }

    private string _scheduleType = "Daily";
    /// <summary>Once | Daily | Weekly | Monthly</summary>
    public string ScheduleType
    {
        get => _scheduleType;
        set
        {
            _scheduleType = value;
            OnPropertyChanged();
            // Show/hide the day-of-month field
            OnPropertyChanged(nameof(IsMonthlyOrOnce));
            OnPropertyChanged(nameof(IsOnce));
        }
    }

    private int _scheduleDay = 1;
    /// <summary>Day of month (Monthly) or DayOfWeek index (Weekly).</summary>
    public int ScheduleDay
    {
        get => _scheduleDay;
        set { _scheduleDay = value; OnPropertyChanged(); }
    }

    private DateTime _nextRunTime = DateTime.Now.AddDays(1);
    public DateTime NextRunTime
    {
        get => _nextRunTime;
        set { _nextRunTime = value; OnPropertyChanged(); }
    }

    private string _backupFolder = string.Empty;
    public string BackupFolder
    {
        get => _backupFolder;
        set { _backupFolder = value; OnPropertyChanged(); }
    }

    private string _mySqlDumpPath = "mysqldump";
    public string MySqlDumpPath
    {
        get => _mySqlDumpPath;
        set { _mySqlDumpPath = value; OnPropertyChanged(); }
    }

    private bool _isBackupEnabled;
    public bool IsBackupEnabled
    {
        get => _isBackupEnabled;
        set { _isBackupEnabled = value; OnPropertyChanged(); }
    }

    private string _backupStatusMessage = string.Empty;
    public string BackupStatusMessage
    {
        get => _backupStatusMessage;
        set { _backupStatusMessage = value; OnPropertyChanged(); }
    }

    // Visibility helpers for XAML
    public bool IsMonthlyOrOnce => ScheduleType == "Monthly" || ScheduleType == "Once";
    public bool IsOnce          => ScheduleType == "Once";

    // ── Commands ─────────────────────────────────────────────────────────────
    public ICommand PresetLocalCommand        { get; }
    public ICommand PresetNetworkCommand      { get; }
    public ICommand PresetRemoteCommand       { get; }
    public ICommand TestBothCommand           { get; }
    public ICommand SaveSettingsCommand       { get; }
    public ICommand FullBackupCommand         { get; }
    public ICommand DifferentialBackupCommand { get; }
    public ICommand IncrementalBackupCommand  { get; }
    public ICommand BrowseBackupFolderCommand  { get; }
    public ICommand BrowseMySqlDumpCommand    { get; }
    public ICommand SaveBackupSettingsCommand { get; }
    public ICommand OpenSystemsProfileCommand { get; }

    // Mode helpers kept for XAML compatibility
    public bool IsLocal  => DatabaseMode == "Local";
    public bool IsLan    => DatabaseMode == "LAN";
    public bool IsRemote => DatabaseMode == "Remote";

    public SettingsViewModel()
    {
        _backupService = new BackupService();
        _scheduler     = new BackupSchedulerService();

        LoadSettings();
        LoadBackupSettings();

        PresetLocalCommand   = new RelayCommand(_ => ApplyPreset("Local"));
        PresetNetworkCommand = new RelayCommand(_ => ApplyPreset("LAN"));
        PresetRemoteCommand  = new RelayCommand(_ => ApplyPreset("Remote"));
        TestBothCommand      = new RelayCommand(async _ => await ExecuteTestBoth());
        SaveSettingsCommand  = new RelayCommand(async _ => await ExecuteSaveSettings(null));

        FullBackupCommand         = new RelayCommand(async _ => await ExecuteManualBackup("Full"));
        DifferentialBackupCommand = new RelayCommand(async _ => await ExecuteManualBackup("Differential"));
        IncrementalBackupCommand  = new RelayCommand(async _ => await ExecuteManualBackup("Incremental"));

        BrowseBackupFolderCommand  = new RelayCommand(_ => BrowseBackupFolder());
        BrowseMySqlDumpCommand    = new RelayCommand(_ => BrowseMySqlDump());
        SaveBackupSettingsCommand = new RelayCommand(_ => ExecuteSaveBackupSettings());
        
        OpenSystemsProfileCommand = new RelayCommand(_ => new GoodGovernanceApp.Views.SystemsApplicationProfile().ShowDialog());
    }

    // ── Preset Logic ─────────────────────────────────────────────────────────
    private void ApplyPreset(string mode)
    {
        DatabaseMode = mode;
        if (mode == "Local") SelectedPreset = "LOCAL";
        else if (mode == "LAN") SelectedPreset = "NETWORK";
        else if (mode == "Remote") SelectedPreset = "REMOTE";

        OnPropertyChanged(nameof(IsLocal));
        OnPropertyChanged(nameof(IsLan));
        OnPropertyChanged(nameof(IsRemote));

        switch (mode)
        {
            case "Local":
                LocalConnectionString  = BuildGgmsConnStr("localhost", "governance");
                LanConnectionString    = BuildGgmsConnStr(LanIp, "governance");
                RemoteConnectionString = BuildGgmsRemoteConnStr();
                CrsServer   = "localhost"; CrsPort = "3306";
                CrsDatabase = "crs_db";   CrsUser = "root"; CrsPassword = "root";
                break;

            case "LAN":
                if (string.IsNullOrWhiteSpace(LanIp))
                    LanIp = "192.168.1.1";
                LanConnectionString = BuildGgmsConnStr(LanIp, "governance");
                CrsServer   = LanIp;  CrsPort = "3306";
                CrsDatabase = "crs_db"; CrsUser = "root"; CrsPassword = "root";
                break;

            case "Remote":
                RemoteConnectionString = BuildGgmsRemoteConnStr();
                CrsServer   = "194.59.164.58";       CrsPort     = "3306";
                CrsDatabase = "u621755393_crs";      CrsUser     = "u621755393_crs_user";
                CrsPassword = "Crs@2026";
                break;
        }

        StatusMessage = $"{mode} preset applied. Edit the LAN IP if needed, then TEST BOTH.";
    }

    private static string BuildGgmsConnStr(string server, string db)
        => $"Server={server};Port=3306;Database={db};User=root;Password=root;" +
           "AllowZeroDateTime=True;ConvertZeroDateTime=True;Connection Timeout=15;SslMode=None;";

    private static string BuildGgmsRemoteConnStr()
        => "Server=194.59.164.58;Port=3306;Database=u621755393_ggms;" +
           "User=u621755393_ggms_user;Password=Ggms@2026;" +
           "AllowZeroDateTime=True;ConvertZeroDateTime=True;Connection Timeout=15;SslMode=None;";

    public string BuildCrsConnStr()
        => $"Server={CrsServer};Port={CrsPort};Database={CrsDatabase};User={CrsUser};" +
           $"Password={CrsPassword};AllowZeroDateTime=True;ConvertZeroDateTime=True;" +
           "Connection Timeout=15;SslMode=None;";

    // ── Connection Test ───────────────────────────────────────────────────────
    private async Task ExecuteTestBoth()
    {
        IsTesting = true;
        GgmsTestResult = "Testing GGMS...";
        CrsTestResult  = "Testing CRS...";

        string ggmsConn = DatabaseMode switch
        {
            "Remote" => RemoteConnectionString,
            "LAN"    => LanConnectionString,
            _        => LocalConnectionString
        };

        var ggmsTask = TestConnectionAsync(ggmsConn);
        var crsTask  = TestConnectionAsync(BuildCrsConnStr());
        await Task.WhenAll(ggmsTask, crsTask);

        var (ggmsOk, ggmsMsg) = ggmsTask.Result;
        var (crsOk,  crsMsg)  = crsTask.Result;

        GgmsTestResult = ggmsOk ? "✅ GGMS: Connected"  : $"❌ GGMS: {ggmsMsg}";
        CrsTestResult  = crsOk  ? "✅ CRS: Connected"   : $"❌ CRS: {crsMsg}";
        IsTesting = false;
    }

    private static async Task<(bool ok, string msg)> TestConnectionAsync(string connStr)
    {
        try
        {
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            return (true, "OK");
        }
        catch (MySqlException ex) { return (false, $"MySQL Error [{ex.Number}]: {ex.Message}"); }
        catch (Exception ex)      { return (false, $"General Error: {ex.Message}"); }
    }

    // ── Load / Save Settings ─────────────────────────────────────────────────
    private void LoadSettings()
    {
        try
        {
            var ggmsConfig = ConfigHelper.ReadConfig("GgmsConfig.txt");
            var crsConfig  = ConfigHelper.ReadConfig("CrsConfig.txt");

            if (crsConfig.ContainsKey("Server"))
            {
                CrsServer   = crsConfig.GetValueOrDefault("Server",   "localhost");
                CrsPort     = crsConfig.GetValueOrDefault("Port",     "3306");
                CrsDatabase = crsConfig.GetValueOrDefault("Database", "crs_db");
                CrsUser     = crsConfig.GetValueOrDefault("User",     "root");
                CrsPassword = crsConfig.GetValueOrDefault("Password", "");
            }

            string currentServer = ggmsConfig.GetValueOrDefault("Server", "localhost");
            if (currentServer == "194.59.164.58")
                ApplyPreset("Remote");
            else if (currentServer != "localhost" && currentServer.Contains('.'))
            { LanIp = currentServer; ApplyPreset("LAN"); }
            else
                ApplyPreset("Local");
        }
        catch { StatusMessage = "Error loading settings from txt config files."; }
    }

    private void LoadBackupSettings()
    {
        var s = BackupConfigHelper.Load();
        BackupType      = s.BackupType;
        ScheduleType    = s.ScheduleType;
        ScheduleDay     = s.ScheduleDay;
        NextRunTime     = s.NextRunTime;
        BackupFolder    = s.BackupFolder;
        MySqlDumpPath   = s.MySqlDumpPath;
        IsBackupEnabled = s.IsEnabled;

        // Start scheduler using the active connection string.
        string connStr = DatabaseMode switch
        {
            "Remote" => RemoteConnectionString,
            "LAN"    => LanConnectionString,
            _        => LocalConnectionString
        };
        _scheduler.Start(s, connStr);
    }

    private async Task ExecuteSaveSettings(object? parameter)
    {
        try
        {
            string ggmsConn = DatabaseMode switch
            {
                "Remote" => RemoteConnectionString,
                "LAN"    => LanConnectionString,
                _        => LocalConnectionString
            };

            IsTesting = true;
            StatusMessage = "Testing connection before saving...";
            var (isOk, msg) = await TestConnectionAsync(ggmsConn);
            IsTesting = false;

            if (!isOk)
            {
                System.Windows.MessageBox.Show(
                    $"Cannot save settings because the database is unreachable.\n\nError:\n{msg}",
                    "Connection Failed",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "Save aborted. Database unreachable.";
                return;
            }

            string server = "localhost", port = "3306", db = "governance", user = "root", pass = "";
            foreach (var part in ggmsConn.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2)
                {
                    string k = kv[0].Trim().ToLower();
                    string v = kv[1].Trim();
                    if (k == "server") server = v;
                    else if (k == "port") port = v;
                    else if (k == "database") db = v;
                    else if (k == "user") user = v;
                    else if (k == "password") pass = v;
                }
            }

            ConfigHelper.WriteConfig("GgmsConfig.txt", server, port, db, user, pass);
            ConfigHelper.WriteConfig("CrsConfig.txt", CrsServer, CrsPort, CrsDatabase, CrsUser, CrsPassword);

            StatusMessage = "Settings successfully saved. Please restart the app.";
            System.Windows.MessageBox.Show(
                "Settings saved successfully!\n\nPlease restart the application to apply the changes.",
                "Restart Required",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch { StatusMessage = "Error saving settings to txt config files."; }
    }

    // ── Backup: Browse + Save ─────────────────────────────────────────────────
    private void BrowseBackupFolder()
    {
        // WPF does not have a FolderBrowserDialog natively.
        // We use a SaveFileDialog pointed at a folder as a well-known workaround.
        var dlg = new SaveFileDialog
        {
            Title            = "Select Backup Destination Folder",
            FileName         = "[Select this folder]",
            Filter           = "Folder|*.none",
            CheckFileExists  = false,
            CheckPathExists  = true,
            ValidateNames    = false
        };
        if (!string.IsNullOrWhiteSpace(BackupFolder) && Directory.Exists(BackupFolder))
            dlg.InitialDirectory = BackupFolder;

        if (dlg.ShowDialog() == true)
            BackupFolder = Path.GetDirectoryName(dlg.FileName) ?? string.Empty;
    }

    private void BrowseMySqlDump()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select mysqldump.exe",
            Filter = "mysqldump.exe|mysqldump.exe|All Executables|*.exe"
        };
        if (dlg.ShowDialog() == true)
            MySqlDumpPath = dlg.FileName;
    }

    private void ExecuteSaveBackupSettings()
    {
        var settings = new BackupSettings
        {
            BackupType   = BackupType,
            ScheduleType = ScheduleType,
            ScheduleDay  = ScheduleDay,
            NextRunTime  = NextRunTime,
            BackupFolder = BackupFolder,
            MySqlDumpPath = MySqlDumpPath,
            IsEnabled    = IsBackupEnabled
        };

        BackupConfigHelper.Save(settings);

        string connStr = DatabaseMode switch
        {
            "Remote" => RemoteConnectionString,
            "LAN"    => LanConnectionString,
            _        => LocalConnectionString
        };

        _scheduler.Start(settings, connStr);

        BackupStatusMessage = IsBackupEnabled
            ? $"✅ Auto backup enabled — next run: {NextRunTime:yyyy-MM-dd HH:mm}"
            : "⏸ Auto backup disabled and settings saved.";
    }

    // ── Manual Backup ─────────────────────────────────────────────────────────
    private async Task ExecuteManualBackup(string type)
    {
        BackupStatusMessage = $"Running {type} backup manually…";

        string folder = string.IsNullOrWhiteSpace(BackupFolder)
            ? Path.Combine(AppContext.BaseDirectory, "Backups")
            : BackupFolder;

        _backupService.MySqlDumpPath = MySqlDumpPath;

        string connStr = DatabaseMode switch
        {
            "Remote" => RemoteConnectionString,
            "LAN"    => LanConnectionString,
            _        => LocalConnectionString
        };

        bool success = type switch
        {
            "Differential" => await _backupService.CreateDifferentialBackupAsync(connStr, folder),
            "Incremental"  => await _backupService.CreateIncrementalBackupAsync(connStr, folder),
            _              => await _backupService.CreateFullBackupAsync(connStr, folder)
        };

        BackupStatusMessage = success
            ? $"✅ {type} backup saved to: {folder}"
            : $"❌ {type} backup failed. Check backup_errors.log in {folder}";
    }
}