using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using GoodGovernanceApp.Data;
using GoodGovernanceApp.Views;
using System.Globalization;
using System.Threading;

namespace GoodGovernanceApp;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }
    public static IConfiguration Config { get; set; } = null!;

    public App()
    {
        var culture = new CultureInfo("en-PH");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoodGovernanceApp");
        Directory.CreateDirectory(appDataFolder);
        string appDataSettingsPath = Path.Combine(appDataFolder, "appsettings.json");

        if (!File.Exists(appDataSettingsPath))
        {
            string baseSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(baseSettingsPath))
                File.Copy(baseSettingsPath, appDataSettingsPath);
        }

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.SetBasePath(appDataFolder);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={Path.Combine(appDataFolder, "ggms.db")}");
                }, ServiceLifetime.Transient, ServiceLifetime.Transient);

                services.AddDbContext<CloudDbContext>((serviceProvider, options) =>
                {
                    try
                    {
                        var connStr = DatabaseConfig.ConnectionString;
                        options.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 31)));
                    }
                    catch
                    {
                        // No valid MySQL connection string configured — use a dummy so DI doesn't crash.
                        // The CloudDbContext will only be used when IsOnline = true.
                        options.UseMySql("Server=localhost;Database=ggms;User=root;Password=;", new MySqlServerVersion(new Version(8, 0, 31)));
                    }
                }, ServiceLifetime.Transient, ServiceLifetime.Transient);

                services.AddSingleton<DatabaseHelper>();
                services.AddSingleton<GoodGovernanceApp.Services.ValidationService>();
                services.AddSingleton<GoodGovernanceApp.Services.SessionService>();
                services.AddSingleton<GoodGovernanceApp.Services.FileService>();
                services.AddSingleton<GoodGovernanceApp.Services.BackupService>();

                services.AddTransient<MainWindow>();
                services.AddTransient<GoodGovernanceApp.Views.LoginWindow>();
                services.AddTransient<GoodGovernanceApp.ViewModels.LoginViewModel>();
            })
            .Build();

        Config = AppHost.Services.GetRequiredService<IConfiguration>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();

        GoodGovernanceApp.Services.ConnectivityService.StartMonitoring();
        GoodGovernanceApp.Services.SyncService.StartAutoSync();

        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GoodGovernanceApp");

        Directory.CreateDirectory(Path.Combine(appData, "Logos"));
        Directory.CreateDirectory(Path.Combine(appData, "ProfilePhotos"));

        base.OnStartup(e);

        // ── Initialise SQLite database BEFORE showing the login window ──────────
        await Task.Run(async () =>
        {
            using var scope = AppHost.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var dbContext = services.GetRequiredService<AppDbContext>();

                // Step 1: Create all SQLite tables from EF model
                bool created = await dbContext.Database.EnsureCreatedAsync();
                System.Diagnostics.Debug.WriteLine($"[DB Init] EnsureCreated result: {created}");

                // ── Step 2: Verify the users table actually exists ────────────────
                bool usersTableExists = false;
                try
                {
                    var conn = dbContext.Database.GetDbConnection();
                    if (conn.State != System.Data.ConnectionState.Open)
                        await conn.OpenAsync();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='users';";
                    var result = await cmd.ExecuteScalarAsync();
                    usersTableExists = result != null;
                    System.Diagnostics.Debug.WriteLine($"[DB Init] 'users' table exists: {usersTableExists}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DB Init] Table check failed: {ex.Message}");
                }

                // ── Step 3: Connectivity check (warning only, not a blocker) ─────
                Exception? connError = null;
                bool canConn = await Task.Run(() =>
                {
                    try { return dbContext.Database.CanConnect(); }
                    catch (Exception ex) { connError = ex; return false; }
                });

                if (!canConn)
                {
                    string dbMode = Config["AppSettings:DatabaseMode"] ?? "Local";
                    if (dbMode != "Local")
                    {
                        string errorMsg = "Cloud/LAN Database Connection Failed!\n\nThe application will continue using the local SQLite database.";
                        if (connError != null)
                        {
                            string realErr = connError.InnerException?.Message ?? connError.Message;
                            errorMsg += $"\n\nError Details:\n{realErr}";
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show(errorMsg, "Cloud Sync Warning",
                                MessageBoxButton.OK, MessageBoxImage.Warning));
                    }
                }

                // ── Step 4: MySQL-only schema patches ────────────────────────────
                string mode = Config["AppSettings:DatabaseMode"] ?? "Local";
                if (mode != "Local")
                {
                    await Task.Run(() =>
                    {
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE tbl_offices ADD COLUMN office_code VARCHAR(30) NULL AFTER name;"); } catch { }
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE tbl_offices ALTER COLUMN active SET DEFAULT 1;"); } catch { }
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE project_details ADD COLUMN voucher_code VARCHAR(10) NULL;"); } catch { }
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE transactions ADD COLUMN voucher_code VARCHAR(10) NULL;"); } catch { }
                        try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE tbl_transaction ADD COLUMN voucher_code VARCHAR(10) NULL;"); } catch { }

                        try
                        {
                            dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 0;");
                            dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS officeallocations;");
                            dbContext.Database.ExecuteSqlRaw(@"
                                CREATE TABLE officeallocations (
                                    Id INT AUTO_INCREMENT PRIMARY KEY,
                                    YearlyBudgetId INT NOT NULL,
                                    office_code VARCHAR(30) NOT NULL,
                                    AllocatedAmount DECIMAL(65,30) NOT NULL,
                                    CONSTRAINT FK_officeallocations_YearlyBudgets FOREIGN KEY (YearlyBudgetId) REFERENCES YearlyBudgets (Id) ON DELETE CASCADE,
                                    CONSTRAINT FK_officeallocations_tbl_offices FOREIGN KEY (office_code) REFERENCES tbl_offices (office_code) ON DELETE CASCADE
                                ) CHARACTER SET=utf8mb4;
                            ");
                            dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 1;");
                        }
                        catch
                        {
                            try { dbContext.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 1;"); } catch { }
                        }
                    });
                }

                // ── Step 5: Seed only after tables are confirmed to exist ─────────
                if (usersTableExists)
                {
                    await Task.Run(() => DatabaseSeeder.SeedData(dbContext));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DB Init] Skipping seed — 'users' table missing after EnsureCreated. Delete ggms.db and restart.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App Startup] DB init error: {ex.Message}\n{ex.StackTrace}");
            }
        });

        // Show login window only AFTER the database is fully initialized
        var loginWindow = AppHost.Services.GetRequiredService<GoodGovernanceApp.Views.LoginWindow>();
        loginWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        AppHost.Dispose();
        base.OnExit(e);
    }
}