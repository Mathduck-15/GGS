using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GoodGovernanceApp.Services;
using GoodGovernanceApp.Utilities;
using MySqlConnector;

namespace GoodGovernanceApp.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly BackupService _backupService;

    // ── Database Mode ────────────────────────────────────────────────────────
    private string _databaseMode = "Local";
    public string DatabaseMode
    {
        get => _databaseMode;
        set { _databaseMode = value; OnPropertyChanged(); }
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

    // ── CRS Connection Fields (individually editable) ────────────────────────
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

    // ── LAN IP (separate editable field for network preset) ──────────────────
    private string _lanIp = "192.168.1.1";
    public string LanIp
    {
        get => _lanIp;
        set
        {
            _lanIp = value;
            OnPropertyChanged();
            // Live-update both LAN strings when user edits the IP
            LanConnectionString = BuildGgmsConnStr(value, "governance");
            CrsServer = value;
        }
    }

    // ── Backup ───────────────────────────────────────────────────────────────
    private string _mySqlDumpPath = "mysqldump";
    public string MySqlDumpPath
    {
        get => _mySqlDumpPath;
        set { _mySqlDumpPath = value; OnPropertyChanged(); }
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

    // ── Commands ─────────────────────────────────────────────────────────────
    public ICommand PresetLocalCommand { get; }
    public ICommand PresetNetworkCommand { get; }
    public ICommand PresetRemoteCommand { get; }
    public ICommand TestBothCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand FullBackupCommand { get; }
    public ICommand DifferentialBackupCommand { get; }
    public ICommand IncrementalBackupCommand { get; }

    // Keep legacy for XAML that still uses IsLocal/IsLan/IsRemote
    public bool IsLocal  => DatabaseMode == "Local";
    public bool IsLan    => DatabaseMode == "LAN";
    public bool IsRemote => DatabaseMode == "Remote";

    public SettingsViewModel()
    {
        _backupService = new BackupService();
        LoadSettings();

        PresetLocalCommand   = new RelayCommand(_ => ApplyPreset("Local"));
        PresetNetworkCommand = new RelayCommand(_ => ApplyPreset("LAN"));
        PresetRemoteCommand  = new RelayCommand(_ => ApplyPreset("Remote"));
        TestBothCommand      = new RelayCommand(async _ => await ExecuteTestBoth());
        SaveSettingsCommand  = new RelayCommand(async _ => await ExecuteSaveSettings(null));

        FullBackupCommand         = new RelayCommand(async p => await ExecuteBackup("Full"));
        DifferentialBackupCommand = new RelayCommand(async p => await ExecuteBackup("Differential"));
        IncrementalBackupCommand  = new RelayCommand(async p => await ExecuteBackup("Incremental"));
    }

    // ── Preset Logic ─────────────────────────────────────────────────────────
    private void ApplyPreset(string mode)
    {
        DatabaseMode = mode;
        OnPropertyChanged(nameof(IsLocal));
        OnPropertyChanged(nameof(IsLan));
        OnPropertyChanged(nameof(IsRemote));

        switch (mode)
        {
            case "Local":
                LocalConnectionString = BuildGgmsConnStr("localhost", "governance");
                LanConnectionString   = BuildGgmsConnStr(LanIp, "governance");
                RemoteConnectionString = BuildGgmsRemoteConnStr();
                CrsServer   = "localhost";
                CrsPort     = "3306";
                CrsDatabase = "crs_db";
                CrsUser     = "root";
                CrsPassword = "root";
                break;

            case "LAN":
                // Keep LanIp editable — only fill defaults if blank
                if (string.IsNullOrWhiteSpace(LanIp) || LanIp == "192.168.1.1")
                    LanIp = "192.168.1.1";  // triggers live-update of LanConnectionString + CrsServer
                else
                    LanConnectionString = BuildGgmsConnStr(LanIp, "governance");

                CrsServer   = LanIp;
                CrsPort     = "3306";
                CrsDatabase = "crs_db";
                CrsUser     = "root";
                CrsPassword = "root";
                break;

            case "Remote":
                RemoteConnectionString = BuildGgmsRemoteConnStr();
                CrsServer   = "194.59.164.58";
                CrsPort     = "3306";
                CrsDatabase = "u621755393_crs";
                CrsUser     = "u621755393_crs_user";
                CrsPassword = "Crs@2026";
                break;
        }

        StatusMessage = $"{mode} preset applied. Edit the LAN IP if needed, then TEST BOTH.";
    }

    private static string BuildGgmsConnStr(string server, string db)
        => $"Server={server};Port=3306;Database={db};User=root;Password=root;AllowZeroDateTime=True;ConvertZeroDateTime=True;Connection Timeout=15;SslMode=None;";

    private static string BuildGgmsRemoteConnStr()
        => "Server=194.59.164.58;Port=3306;Database=u621755393_ggms;User=u621755393_ggms_user;Password=Ggms@2026;AllowZeroDateTime=True;ConvertZeroDateTime=True;Connection Timeout=15;SslMode=None;";

    public string BuildCrsConnStr()
        => $"Server={CrsServer};Port={CrsPort};Database={CrsDatabase};User={CrsUser};Password={CrsPassword};AllowZeroDateTime=True;ConvertZeroDateTime=True;Connection Timeout=15;SslMode=None;";

    // ── Test Both ────────────────────────────────────────────────────────────
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

        // Always test the CURRENT UI fields, not the saved file, 
        // to allow users to verify their typing before they hit Save!
        string crsConn = BuildCrsConnStr();

        var ggmsTask = TestConnectionAsync(ggmsConn);
        var crsTask  = TestConnectionAsync(crsConn);
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
        catch (MySqlException ex)
        {
            // Extract detailed MySQL error information
            string detailedError = $"MySQL Error [{ex.Number}]: {ex.Message}";
            
            if (ex.InnerException != null)
            {
                detailedError += $"\nInner: {ex.InnerException.Message}";
            }
            
            return (false, detailedError);
        }
        catch (Exception ex)
        {
            return (false, $"General Error: {ex.Message}");
        }
    }

    // ── Load / Save ──────────────────────────────────────────────────────────
    private void LoadSettings()
    {
        try
        {
            var ggmsConfig = ConfigHelper.ReadConfig("GgmsConfig.txt");
            var crsConfig = ConfigHelper.ReadConfig("CrsConfig.txt");

            if (crsConfig.ContainsKey("Server"))
            {
                CrsServer   = crsConfig.GetValueOrDefault("Server", "localhost");
                CrsPort     = crsConfig.GetValueOrDefault("Port", "3306");
                CrsDatabase = crsConfig.GetValueOrDefault("Database", "crs_db");
                CrsUser     = crsConfig.GetValueOrDefault("User", "root");
                CrsPassword = crsConfig.GetValueOrDefault("Password", "");
            }

            // Figure out which preset we are currently on based on Ggms Server
            string currentServer = ggmsConfig.GetValueOrDefault("Server", "localhost");
            
            if (currentServer == "194.59.164.58")
            {
                ApplyPreset("Remote");
            }
            else if (currentServer != "localhost" && currentServer.Contains("."))
            {
                LanIp = currentServer;
                ApplyPreset("LAN");
            }
            else
            {
                ApplyPreset("Local");
            }
        }
        catch { StatusMessage = "Error loading settings from txt config files."; }
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

            // Test before allowing save
            var (isOk, msg) = await TestConnectionAsync(ggmsConn);
            IsTesting = false;

            if (!isOk)
            {
                System.Windows.MessageBox.Show(
                    $"Cannot save settings because the database is unreachable.\n\nError:\n{msg}",
                    "Connection Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "Save aborted. Database unreachable.";
                return;
            }

            // Parse the selected GGMS connection string to save its parts
            string server= "localhost", port= "3306", db= "governance", user= "root", pass= "";
            
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
                "Restart Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch { StatusMessage = "Error saving settings to txt config files."; }
    }

    // ── Backup ───────────────────────────────────────────────────────────────
    private async Task ExecuteBackup(string type)
    {
        StatusMessage = $"Starting {type} backup...";
        string backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups");

        string connStr = DatabaseMode switch
        {
            "Remote" => RemoteConnectionString,
            "LAN"    => LanConnectionString,
            _        => LocalConnectionString
        };

        _backupService.MySqlDumpPath = MySqlDumpPath;

        bool success = type switch
        {
            "Full"          => await _backupService.CreateFullBackupAsync(connStr, backupDir),
            "Differential"  => await _backupService.CreateDifferentialBackupAsync(connStr, backupDir),
            "Incremental"   => await _backupService.CreateIncrementalBackupAsync(connStr, backupDir),
            _               => false
        };

        StatusMessage = success
            ? $"{type} backup completed successfully in ./Backups/"
            : $"Failed to create {type} backup. Check if mysqldump is installed.";
    }
}