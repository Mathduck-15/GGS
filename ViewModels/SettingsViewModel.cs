using GoodGovernanceApp.Models;
using GoodGovernanceApp.Services;
using GoodGovernanceApp.Utilities;
using GoodGovernanceApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using MySqlConnector;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GoodGovernanceApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        // ── Fields ───────────────────────────────────────────────────────────
        private readonly SessionService _sessionService;
        private readonly BackupService _backupService;
        private readonly BackupSchedulerService _scheduler;


        public SettingsViewModel()
      : this(App.AppHost!.Services.GetRequiredService<SessionService>())
        {
        }
        // ── Constructor ──────────────────────────────────────────────────────
        public SettingsViewModel(SessionService sessionService)
        {
            _sessionService = sessionService;
            _backupService = new BackupService();
            _scheduler = new BackupSchedulerService();

            LoadSettings();
            LoadBackupSettings();

            // ── Connection commands ──────────────────────────────────────────
            PresetLocalCommand = new RelayCommand(_ => ApplyPreset("Local"));
            PresetNetworkCommand = new RelayCommand(_ => ApplyPreset("LAN"));
            PresetRemoteCommand = new RelayCommand(_ => ApplyPreset("Remote"));
            TestBothCommand = new RelayCommand(async _ => await ExecuteTestBoth());
            SaveSettingsCommand = new RelayCommand(async _ => await ExecuteSaveSettings(null));

            // ── Backup commands — all gated behind OTP ───────────────────────
            SaveBackupSettingsCommand = new RelayCommand(async _ => await ExecuteWithOtpAsync(ExecuteSaveBackupSettingsAsync));
            FullBackupCommand = new RelayCommand(async _ => await ExecuteWithOtpAsync(() => ExecuteManualBackupAsync("Full")));
            DifferentialBackupCommand = new RelayCommand(async _ => await ExecuteWithOtpAsync(() => ExecuteManualBackupAsync("Differential")));
            IncrementalBackupCommand = new RelayCommand(async _ => await ExecuteWithOtpAsync(() => ExecuteManualBackupAsync("Incremental")));

            // ── Other commands ───────────────────────────────────────────────
            BrowseBackupFolderCommand = new RelayCommand(_ => BrowseBackupFolder());
            BrowseMySqlDumpCommand = new RelayCommand(_ => BrowseMySqlDump());
            OpenSystemsProfileCommand = new RelayCommand(_ => new SystemsApplicationProfile().ShowDialog());
        }

        // ── OTP Gate ─────────────────────────────────────────────────────────
        /// <summary>
        /// Opens the OTP verification window using the logged-in user's email.
        /// Only proceeds with <paramref name="action"/> if the OTP is verified.
        /// </summary>
        private async Task ExecuteWithOtpAsync(Func<Task> action)
        {
            string? email = _sessionService.CurrentUser?.Email;

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show(
                    "No email address is linked to your account.\nCannot send OTP — please contact your administrator.",
                    "OTP Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var otpWindow = new OtpVerificationWindow(email);
            otpWindow.ShowDialog();

            if (otpWindow.IsVerified)
            {
                await action();
            }
            else
            {
                BackupStatusMessage = "⚠ Action cancelled — OTP was not verified.";
            }
        }

        // ── Database Mode ────────────────────────────────────────────────────
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

        // ── GGMS Connection Strings ──────────────────────────────────────────
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

        // ── CRS Connection Fields ────────────────────────────────────────────
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

        // ── LAN IP ───────────────────────────────────────────────────────────
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

        // ── Status ───────────────────────────────────────────────────────────
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

        // ── Backup Settings ──────────────────────────────────────────────────
        private string _backupType = "Full";
        public string BackupType
        {
            get => _backupType;
            set { _backupType = value; OnPropertyChanged(); }
        }

        private string _scheduleType = "Daily";
        public string ScheduleType
        {
            get => _scheduleType;
            set
            {
                _scheduleType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMonthlyOrOnce));
                OnPropertyChanged(nameof(IsOnce));
            }
        }

        private int _scheduleDay = 1;
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
        public bool IsOnce => ScheduleType == "Once";

        // Mode helpers for XAML
        public bool IsLocal => DatabaseMode == "Local";
        public bool IsLan => DatabaseMode == "LAN";
        public bool IsRemote => DatabaseMode == "Remote";

        // ── Commands ─────────────────────────────────────────────────────────
        public ICommand PresetLocalCommand { get; }
        public ICommand PresetNetworkCommand { get; }
        public ICommand PresetRemoteCommand { get; }
        public ICommand TestBothCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand SaveBackupSettingsCommand { get; }
        public ICommand FullBackupCommand { get; }
        public ICommand DifferentialBackupCommand { get; }
        public ICommand IncrementalBackupCommand { get; }
        public ICommand BrowseBackupFolderCommand { get; }
        public ICommand BrowseMySqlDumpCommand { get; }
        public ICommand OpenSystemsProfileCommand { get; }

        // ── Preset Logic ─────────────────────────────────────────────────────
        private void ApplyPreset(string mode)
        {
            DatabaseMode = mode;
            SelectedPreset = mode switch
            {
                "Local" => "LOCAL",
                "LAN" => "NETWORK",
                "Remote" => "REMOTE",
                _ => string.Empty
            };

            OnPropertyChanged(nameof(IsLocal));
            OnPropertyChanged(nameof(IsLan));
            OnPropertyChanged(nameof(IsRemote));

            switch (mode)
            {
                case "Local":
                    LocalConnectionString = BuildGgmsConnStr("localhost", "governance");
                    LanConnectionString = BuildGgmsConnStr(LanIp, "governance");
                    RemoteConnectionString = BuildGgmsRemoteConnStr();
                    CrsServer = "localhost"; CrsPort = "3306";
                    CrsDatabase = "crs_db"; CrsUser = "root"; CrsPassword = "root";
                    break;

                case "LAN":
                    if (string.IsNullOrWhiteSpace(LanIp)) LanIp = "192.168.1.1";
                    LanConnectionString = BuildGgmsConnStr(LanIp, "governance");
                    CrsServer = LanIp; CrsPort = "3306";
                    CrsDatabase = "crs_db"; CrsUser = "root"; CrsPassword = "root";
                    break;

                case "Remote":
                    RemoteConnectionString = BuildGgmsRemoteConnStr();
                    CrsServer = "194.59.164.58"; CrsPort = "3306";
                    CrsDatabase = "u621755393_crs"; CrsUser = "u621755393_crs_user";
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

        private string ActiveGgmsConnStr => DatabaseMode switch
        {
            "Remote" => RemoteConnectionString,
            "LAN" => LanConnectionString,
            _ => LocalConnectionString
        };

        // ── Connection Test ───────────────────────────────────────────────────
        private async Task ExecuteTestBoth()
        {
            IsTesting = true;
            GgmsTestResult = "Testing GGMS...";
            CrsTestResult = "Testing CRS...";

            var ggmsTask = TestConnectionAsync(ActiveGgmsConnStr);
            var crsTask = TestConnectionAsync(BuildCrsConnStr());
            await Task.WhenAll(ggmsTask, crsTask);

            var (ggmsOk, ggmsMsg) = ggmsTask.Result;
            var (crsOk, crsMsg) = crsTask.Result;

            GgmsTestResult = ggmsOk ? "✅ GGMS: Connected" : $"❌ GGMS: {ggmsMsg}";
            CrsTestResult = crsOk ? "✅ CRS: Connected" : $"❌ CRS: {crsMsg}";
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
            catch (Exception ex) { return (false, $"General Error: {ex.Message}"); }
        }

        // ── Load / Save Settings ─────────────────────────────────────────────
        private void LoadSettings()
        {
            try
            {
                var ggmsConfig = ConfigHelper.ReadConfig("GgmsConfig.txt");
                var crsConfig = ConfigHelper.ReadConfig("CrsConfig.txt");

                if (crsConfig.ContainsKey("Server"))
                {
                    CrsServer = crsConfig.GetValueOrDefault("Server", "localhost");
                    CrsPort = crsConfig.GetValueOrDefault("Port", "3306");
                    CrsDatabase = crsConfig.GetValueOrDefault("Database", "crs_db");
                    CrsUser = crsConfig.GetValueOrDefault("User", "root");
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
            catch { StatusMessage = "Error loading settings from config files."; }
        }

        private void LoadBackupSettings()
        {
            var s = BackupConfigHelper.Load();
            BackupType = s.BackupType;
            ScheduleType = s.ScheduleType;
            ScheduleDay = s.ScheduleDay;
            NextRunTime = s.NextRunTime;
            BackupFolder = s.BackupFolder;
            MySqlDumpPath = s.MySqlDumpPath;
            IsBackupEnabled = s.IsEnabled;

            _scheduler.Start(s, ActiveGgmsConnStr);
        }

        private async Task ExecuteSaveSettings(object? parameter)
        {
            try
            {
                IsTesting = true;
                StatusMessage = "Testing connection before saving...";
                var (isOk, msg) = await TestConnectionAsync(ActiveGgmsConnStr);
                IsTesting = false;

                if (!isOk)
                {
                    MessageBox.Show(
                        $"Cannot save — database is unreachable.\n\nError:\n{msg}",
                        "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Save aborted. Database unreachable.";
                    return;
                }

                string server = "localhost", port = "3306", db = "governance", user = "root", pass = "";
                foreach (var part in ActiveGgmsConnStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length != 2) continue;
                    string k = kv[0].Trim().ToLower(), v = kv[1].Trim();
                    if (k == "server") server = v;
                    else if (k == "port") port = v;
                    else if (k == "database") db = v;
                    else if (k == "user") user = v;
                    else if (k == "password") pass = v;
                }

                ConfigHelper.WriteConfig("GgmsConfig.txt", server, port, db, user, pass);
                ConfigHelper.WriteConfig("CrsConfig.txt", CrsServer, CrsPort, CrsDatabase, CrsUser, CrsPassword);

                StatusMessage = "Settings saved. Please restart the application.";
                MessageBox.Show(
                    "Settings saved successfully!\n\nPlease restart the application to apply the changes.",
                    "Restart Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { StatusMessage = "Error saving settings to config files."; }
        }

        // ── Backup: Browse ────────────────────────────────────────────────────
        private void BrowseBackupFolder()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Select Backup Destination Folder",
                FileName = "[Select this folder]",
                Filter = "Folder|*.none",
                CheckFileExists = false,
                CheckPathExists = true,
                ValidateNames = false
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
                Title = "Select mysqldump.exe",
                Filter = "mysqldump.exe|mysqldump.exe|All Executables|*.exe"
            };
            if (dlg.ShowDialog() == true)
                MySqlDumpPath = dlg.FileName;
        }

        // ── Backup: Save Settings (OTP-gated) ────────────────────────────────
        private Task ExecuteSaveBackupSettingsAsync()
        {
            var settings = new BackupSettings
            {
                BackupType = BackupType,
                ScheduleType = ScheduleType,
                ScheduleDay = ScheduleDay,
                NextRunTime = NextRunTime,
                BackupFolder = BackupFolder,
                MySqlDumpPath = MySqlDumpPath,
                IsEnabled = IsBackupEnabled
            };

            BackupConfigHelper.Save(settings);
            _scheduler.Start(settings, ActiveGgmsConnStr);

            BackupStatusMessage = IsBackupEnabled
                ? $"✅ Auto backup enabled — next run: {NextRunTime:yyyy-MM-dd HH:mm}"
                : "⏸ Auto backup disabled and settings saved.";

            return Task.CompletedTask;
        }

        // ── Backup: Manual Run (OTP-gated) ────────────────────────────────────
        private async Task ExecuteManualBackupAsync(string type)
        {
            BackupStatusMessage = $"⏳ Running {type} backup...";

            string folder = string.IsNullOrWhiteSpace(BackupFolder)
                ? Path.Combine(AppContext.BaseDirectory, "Backups")
                : BackupFolder;

            _backupService.MySqlDumpPath = MySqlDumpPath;

            bool success = type switch
            {
                "Differential" => await _backupService.CreateDifferentialBackupAsync(ActiveGgmsConnStr, folder),
                "Incremental" => await _backupService.CreateIncrementalBackupAsync(ActiveGgmsConnStr, folder),
                _ => await _backupService.CreateFullBackupAsync(ActiveGgmsConnStr, folder)
            };

            BackupStatusMessage = success
                ? $"✅ {type} backup saved to: {folder}"
                : $"❌ {type} backup failed. Check backup_errors.log in {folder}";
        }
    }
}